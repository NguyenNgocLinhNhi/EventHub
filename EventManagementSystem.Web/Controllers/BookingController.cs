using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;

namespace EventManagementSystem.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // Bước 1: Hiển thị sơ đồ ghế
        public async Task<IActionResult> BookingProcess(int id)
        {
            var @event = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            var bookedSeats = await _context.BookingDetails
                .Where(d => d.Booking.EventId == id &&
                            (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending") &&
                            !d.IsCancelled) // Ghế đã hủy thì không tính là đã đặt
                .Select(d => d.SeatNumber)
                .ToListAsync();

            ViewBag.BookedSeats = bookedSeats;
            return View(@event);
        }

        // Bước 2: Nhận dữ liệu và chuyển sang trang thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(int eventId, string customerName, string customerEmail, string phoneNumber, string selectedSeats, decimal totalAmount)
        {
            var @event = await _context.Events.FindAsync(eventId);
            if (@event == null) return NotFound();

            ViewBag.EventTitle = @event.Title;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerEmail = customerEmail;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.SelectedSeats = selectedSeats;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.EventId = eventId;

            return View("PaymentMethod");
        }

        // Bước 3: Tạo đơn hàng tạm thời (Pending) và trừ kho vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int eventId, string customerName, string customerEmail, string phoneNumber, string selectedSeats, decimal totalAmount, string paymentMethod)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var @event = await _context.Events
                    .Include(e => e.TicketTypes)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null) return BadRequest("Event not found.");

                var seatArray = selectedSeats.Split(',', StringSplitOptions.RemoveEmptyEntries);

                var existingSeats = await _context.BookingDetails
                    .Where(d => d.Booking.EventId == eventId &&
                                (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending") &&
                                !d.IsCancelled) // Bỏ qua các ghế đã hủy để người khác có thể đặt lại
                    .Select(d => d.SeatNumber)
                    .ToListAsync();

                foreach (var seat in seatArray)
                {
                    if (existingSeats.Contains(seat.Trim()))
                    {
                        return BadRequest($"Seat {seat} has already been taken. Please try again.");
                    }
                }

                var user = await _userManager.FindByEmailAsync(customerEmail);
                var booking = new Booking
                {
                    EventId = eventId,
                    BookingDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentMethod = paymentMethod, // Đã bổ sung lưu phương thức thanh toán
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    PhoneNumber = phoneNumber,
                    UserId = user?.Id
                };

                foreach (var seatCode in seatArray)
                {
                    string row = seatCode.Trim().Substring(0, 1).ToUpper();
                    int typeIndex = (row == "A" || row == "B") ? 0 : (row == "C" || row == "D" || row == "E") ? 1 : 2;

                    var ticketTypesOrdered = @event.TicketTypes.OrderByDescending(t => t.Price).ToList();
                    if (typeIndex >= ticketTypesOrdered.Count) typeIndex = ticketTypesOrdered.Count - 1;

                    var ticketType = ticketTypesOrdered.ElementAt(typeIndex);
                    if (ticketType.Quantity <= 0) return BadRequest($"Ticket category {ticketType.Name} is sold out.");

                    booking.BookingDetails.Add(new BookingDetail
                    {
                        TicketTypeId = ticketType.Id,
                        SeatNumber = seatCode.Trim(),
                        Quantity = 1,
                        UnitPrice = ticketType.Price,
                        // Quan trọng: Tạo TicketCode ngay từ lúc này để quản lý
                        TicketCode = "EH-" + Guid.NewGuid().ToString().ToUpper().Substring(0, 8),
                        IsCheckedIn = false // Mặc định chưa tham gia
                    });

                    ticketType.Quantity -= 1;
                    _context.TicketTypes.Update(ticketType);
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ViewBag.BookingId = booking.Id;
                ViewBag.TotalAmount = totalAmount;
                ViewBag.PaymentMethod = paymentMethod;
                ViewBag.SelectedSeats = selectedSeats;

                return View("PaymentQR");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while processing your booking.");
            }
        }

        // Bước 4: Hoàn tất và gửi Email QR rời từng vé
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeBooking(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            // Update official status
            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // GENERATE TICKET CONTENT FOR EMAIL
            var ticketListHtml = new StringBuilder();

            foreach (var detail in booking.BookingDetails)
            {
                // Generate the absolute URL for the cancellation action
                string cancelUrl = Url.Action("CancelTicket", "Booking", new { ticketCode = detail.TicketCode }, Request.Scheme);

               // Use the saved ticket code to generate the QR
                string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={detail.TicketCode}";

                ticketListHtml.Append($@"
            <div style='border: 2px dashed #006D5B; padding: 20px; margin-bottom: 20px; border-radius: 10px; background-color: #fff; font-family: sans-serif;'>
                <h2 style='color: #006D5B; text-align: center; margin-top: 0;'>EVENT TICKET</h2>
                <p><b>Event:</b> {booking.Event?.Title}</p>
                <p><b>Seat Number:</b> <span style='font-size: 20px; color: #ce1212;'>{detail.SeatNumber}</span></p>
                <p><b>Validation Code:</b> {detail.TicketCode}</p>
                <div style='text-align: center; margin-top: 15px;'>
                    <img src='{qrUrl}' width='150' alt='QR Code' />
                    <p style='font-size: 12px; color: #666;'>Please present this code at the entrance for Check-in</p>
                    <div style='margin-top: 15px;'>
                        <a href='{cancelUrl}' style='display: inline-block; padding: 10px 20px; background-color: #dc3545; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 14px;'>
                            Cancel This Ticket (100% Refund)
                        </a>
                    </div>
                </div>
            </div>");
            }

            string subject = $"[EventHub] Booking Confirmation Success #{booking.Id}";
            string body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; background-color: #f9f9f9;'>
            <h3 style='color: #333;'>Thank you, {booking.CustomerName}!</h3>
            <p>Your payment was successful. Below is your electronic ticket list:</p>
            {ticketListHtml}
            <div style='background-color: #eee; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                <p style='margin: 5px 0;'><b>Time:</b> {booking.Event?.StartDate:MMMM dd, yyyy HH:mm}</p>
                <p style='margin: 5px 0;'><b>Location:</b> {booking.Event?.Location}</p>
            </div>
            <p style='text-align: center; color: #888; font-size: 12px; margin-top: 20px;'>This is an automated email, please do not reply.</p>
        </div>";

            await _emailService.SendEmailAsync(booking.CustomerEmail, subject, body);

            return RedirectToAction("BookingSuccess", new { id = booking.Id });
        }

        // Bước 5: Kết quả thành công
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpGet]
        public IActionResult CheckEmailReal(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                    return Json(new { isReal = false });

                string domain = email.Split('@')[1];

                // Kiểm tra xem Domain có máy chủ nhận thư (MX Record) hay không
                // Nếu domain không tồn tại hoặc là domain ảo không có server mail, lệnh này sẽ báo lỗi
                var hostEntry = Dns.GetHostEntry(domain);
                bool hasActiveDomain = hostEntry.AddressList.Length > 0;

                return Json(new { isReal = hasActiveDomain });
            }
            catch
            {
                // Trả về false nếu không tìm thấy tên miền trong hệ thống DNS
                return Json(new { isReal = false });
            }
        }

        // Action mới để xử lý việc quay lại từ trang QR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RollbackAndChangeMethod(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Status == "Pending");

            if (booking != null)
            {
                // Hoàn trả số lượng vé vào kho
                foreach (var detail in booking.BookingDetails)
                {
                    var ticketType = await _context.TicketTypes.FindAsync(detail.TicketTypeId);
                    if (ticketType != null)
                    {
                        ticketType.Quantity += 1;
                        _context.TicketTypes.Update(ticketType);
                    }
                }

                // Lưu thông tin cần thiết để quay lại trang trước
                var eventId = booking.EventId;
                var name = booking.CustomerName;
                var email = booking.CustomerEmail;
                var phone = booking.PhoneNumber;
                var seats = string.Join(",", booking.BookingDetails.Select(d => d.SeatNumber));
                var total = booking.TotalAmount;

                // Xóa đơn hàng Pending
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                // Quay lại trang PaymentMethod
                var @event = await _context.Events.FindAsync(eventId);
                ViewBag.EventTitle = @event?.Title;
                ViewBag.CustomerName = name;
                ViewBag.CustomerEmail = email;
                ViewBag.PhoneNumber = phone;
                ViewBag.SelectedSeats = seats;
                ViewBag.TotalAmount = total;
                ViewBag.EventId = eventId;

                return View("PaymentMethod");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> CancelTicket(string ticketCode)
        {
            var detail = await _context.BookingDetails
                .Include(d => d.Booking).ThenInclude(b => b.Event)
                .Include(d => d.TicketType)
                .FirstOrDefaultAsync(d => d.TicketCode == ticketCode);

            if (detail == null) return NotFound("Ticket code not found.");
            if (detail.IsCancelled) return BadRequest("This ticket is already cancelled.");

            // Kiểm tra điều kiện 24h
            var eventStartTime = detail.Booking.Event.StartDate;
            if ((eventStartTime - DateTime.Now).TotalHours < 24)
                return BadRequest("Cannot cancel within 24 hours of the event.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. CẬP NHẬT TRẠNG THÁI (Thay vì xóa)
                detail.IsCancelled = true;

                // GÁN THỜI GIAN HỦY THỰC TẾ TẠI ĐÂY
                detail.CancelledAt = DateTime.Now;

                // 2. Hoàn trả số lượng vào kho
                detail.TicketType.Quantity += 1;

                // 3. Kiểm tra xem tất cả vé trong đơn hàng đã hủy hết chưa
                var allCancelled = await _context.BookingDetails
                    .Where(d => d.BookingId == detail.BookingId)
                    .AllAsync(d => d.IsCancelled);

                if (allCancelled)
                {
                    detail.Booking.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. GỬI EMAIL (Bổ sung thông tin sự kiện)
                await SendCancellationEmail(detail);

                TempData["Message"] = "Ticket successfully cancelled and invalidated.";
                return RedirectToAction("BookingSuccess", new { id = detail.BookingId });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error processing cancellation.");
            }
        }

        private async Task SendCancellationEmail(BookingDetail detail)
        {
            var booking = detail.Booking;
            var @event = booking.Event;

            string subject = $"[EventHub] Cancellation Confirmed: {@event?.Title}";
            string body = $@"
        <div style='font-family: Arial; padding: 20px; border: 1px solid #eee;'>
            <h2 style='color: #dc3545;'>Cancellation Confirmed</h2>
            <p>Hello <b>{booking.CustomerName}</b>,</p>
            <p>You have successfully cancelled a ticket for the following event:</p>
            <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #dc3545;'>
                <p style='margin:0'><b>Event:</b> {@event?.Title}</p>
                <p style='margin:0'><b>Date:</b> {@event?.StartDate:dd/MM/yyyy HH:mm}</p>
                <p style='margin:0'><b>Location:</b> {@event?.Location}</p>
                <p style='margin:0'><b>Seat:</b> <span style='color:red'>{detail.SeatNumber}</span></p>
            </div>
            <p><b>Refund Status:</b> 100% refund has been initiated to your original payment method.</p>
            <p style='font-size: 12px; color: #888;'>This ticket is now VOID and cannot be used for entry.</p>
        </div>";

            await _emailService.SendEmailAsync(booking.CustomerEmail, subject, body);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelFullBooking(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound("Booking not found.");

            // Condition: 24-hour check
            if ((booking.Event.StartDate - DateTime.Now).TotalHours < 24)
            {
                return BadRequest("This booking cannot be canceled this close to the event start time.");
            }

            foreach (var detail in booking.BookingDetails)
            {
                if (!detail.IsCancelled) // Chỉ xử lý những vé chưa bị hủy lẻ trước đó
                {
                    detail.IsCancelled = true;

                    if (detail.TicketType != null)
                    {
                        detail.TicketType.Quantity += 1;
                        _context.TicketTypes.Update(detail.TicketType);
                    }
                }
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Message"] = "Full booking has been canceled and refunded.";
            return RedirectToAction("BookingSuccess", new { id = bookingId });
        }
    }
}
