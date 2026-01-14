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
                .Where(d => d.Booking.EventId == id && (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending"))
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

                if (@event == null) return BadRequest("Sự kiện không tồn tại.");

                var seatArray = selectedSeats.Split(',', StringSplitOptions.RemoveEmptyEntries);

                var existingSeats = await _context.BookingDetails
                    .Where(d => d.Booking.EventId == eventId && (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending"))
                    .Select(d => d.SeatNumber)
                    .ToListAsync();

                foreach (var seat in seatArray)
                {
                    if (existingSeats.Contains(seat.Trim()))
                    {
                        return BadRequest($"Ghế {seat} vừa có người khác đặt. Vui lòng thử lại.");
                    }
                }

                var user = await _userManager.FindByEmailAsync(customerEmail);
                var booking = new Booking
                {
                    EventId = eventId,
                    BookingDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    PaymentMethod = paymentMethod, // Đã bổ sung lưu phương thức thanh toán [cite: 5]
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
                    if (ticketType.Quantity <= 0) return BadRequest($"Hạng vé {ticketType.Name} đã hết.");

                    booking.BookingDetails.Add(new BookingDetail
                    {
                        TicketTypeId = ticketType.Id,
                        SeatNumber = seatCode.Trim(),
                        Quantity = 1,
                        UnitPrice = ticketType.Price,
                        // Quan trọng: Tạo TicketCode ngay từ lúc này để quản lý [cite: 2]
                        TicketCode = "EH-" + Guid.NewGuid().ToString().ToUpper().Substring(0, 8),
                        IsCheckedIn = false // Mặc định chưa tham gia [cite: 3]
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
                return StatusCode(500, "Lỗi xử lý đơn hàng.");
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

            // Cập nhật trạng thái chính thức [cite: 1]
            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // TẠO NỘI DUNG VÉ CHO EMAIL
            var ticketListHtml = new StringBuilder();

            foreach (var detail in booking.BookingDetails)
            {
                // Sử dụng mã soát vé đã lưu trong DB để tạo QR [cite: 2, 4]
                string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={detail.TicketCode}";

                ticketListHtml.Append($@"
                    <div style='border: 2px dashed #006D5B; padding: 20px; margin-bottom: 20px; border-radius: 10px; background-color: #fff; font-family: sans-serif;'>
                        <h2 style='color: #006D5B; text-align: center; margin-top: 0;'>VÉ XEM SỰ KIỆN</h2>
                        <p><b>Sự kiện:</b> {booking.Event?.Title}</p>
                        <p><b>Vị trí ghế:</b> <span style='font-size: 20px; color: #ce1212;'>{detail.SeatNumber}</span></p>
                        <p><b>Mã soát vé:</b> {detail.TicketCode}</p>
                        <div style='text-align: center; margin-top: 15px;'>
                            <img src='{qrUrl}' width='150' alt='QR Code' />
                            <p style='font-size: 12px; color: #666;'>Vui lòng trình mã này tại cửa sự kiện để Check-in</p>
                        </div>
                    </div>");
            }

            string subject = $"[EventHub] Xác nhận đặt vé thành công #{booking.Id}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; background-color: #f9f9f9;'>
                    <h3 style='color: #333;'>Cảm ơn {booking.CustomerName}!</h3>
                    <p>Thanh toán thành công. Dưới đây là danh sách vé điện tử của bạn:</p>
                    {ticketListHtml}
                    <div style='background-color: #eee; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                        <p style='margin: 5px 0;'><b>Thời gian:</b> {booking.Event?.StartDate:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0;'><b>Địa điểm:</b> {booking.Event?.Location}</p>
                    </div>
                    <p style='text-align: center; color: #888; font-size: 12px; margin-top: 20px;'>Đây là email tự động, vui lòng không phản hồi.</p>
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
    }
}
