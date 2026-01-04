using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

            // Lưu thông tin vào ViewBag để hiển thị ở trang PaymentMethod
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

                // KIỂM TRA TRÙNG GHẾ LẦN CUỐI
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

                // KHỞI TẠO ĐƠN HÀNG (Cho phép mua không cần đăng nhập)
                var user = await _userManager.FindByEmailAsync(customerEmail);
                var booking = new Booking
                {
                    EventId = eventId,
                    BookingDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    PhoneNumber = phoneNumber,
                    UserId = user?.Id // Tự động liên kết nếu email đã có tài khoản
                };

                // XỬ LÝ HẠNG VÉ THEO HÀNG GHẾ
                foreach (var seatCode in seatArray)
                {
                    string row = seatCode.Trim().Substring(0, 1).ToUpper();
                    // Phân loại: A,B (VIP) | C,D,E (Standard) | Còn lại (Economy)
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
                        UnitPrice = ticketType.Price
                    });

                    ticketType.Quantity -= 1; // Inventory Management
                    _context.TicketTypes.Update(ticketType);
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Dữ liệu hiển thị trang QR
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
        public async Task<IActionResult> FinalizeBooking(int bookingId, string selectedSeats)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // TẠO VÉ RỜI KÈM QR CHO TỪNG GHẾ
            var seatList = selectedSeats.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var ticketListHtml = new StringBuilder();

            foreach (var seat in seatList)
            {
                string individualTicketCode = Guid.NewGuid().ToString().ToUpper().Substring(0, 8);
                // Sử dụng API QR Code mã hóa mã soát vé
                string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=TICKET_{individualTicketCode}";

                ticketListHtml.Append($@"
                    <div style='border: 2px dashed #006D5B; padding: 20px; margin-bottom: 20px; border-radius: 10px; background-color: #fff;'>
                        <h2 style='color: #006D5B; text-align: center;'>VÉ XEM SỰ KIỆN</h2>
                        <p><b>Sự kiện:</b> {booking.Event?.Title}</p>
                        <p><b>Vị trí ghế:</b> <span style='font-size: 20px; color: #ce1212;'>{seat.Trim()}</span></p>
                        <p><b>Mã soát vé:</b> {individualTicketCode}</p>
                        <div style='text-align: center;'>
                            <img src='{qrUrl}' width='150' alt='QR Code' />
                        </div>
                    </div>");
            }

            string subject = $"[EventHub] Xác nhận đặt vé thành công #{booking.Id}";
            string body = $@"
                <div style='font-family: Arial; max-width: 600px; margin: auto;'>
                    <h3>Cảm ơn {booking.CustomerName}!</h3>
                    <p>Đơn hàng của bạn đã hoàn tất. Dưới đây là vé của bạn:</p>
                    {ticketListHtml}
                    <p><b>Thời gian:</b> {booking.Event?.StartDate:dd/MM/yyyy HH:mm}</p>
                    <p><b>Địa điểm:</b> {booking.Event?.Location}</p>
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
    }
}