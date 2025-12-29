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

        // Bước 1: Hiển thị sơ đồ ghế và nạp danh sách ghế đã bị chiếm
        public async Task<IActionResult> BookingProcess(int id)
        {
            var @event = await _context.Events
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            // Lấy danh sách ghế đã có chủ (Confirmed) hoặc đang được giữ chỗ (Pending) để hiển thị màu xám
            var bookedSeats = await _context.BookingDetails
                .Where(d => d.Booking.EventId == id && (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending"))
                .Select(d => d.SeatNumber)
                .ToListAsync();

            ViewBag.BookedSeats = bookedSeats;

            return View(@event);
        }

        // Bước 2: Nhận dữ liệu đặt chỗ tạm thời và chuyển sang trang thanh toán
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

        // Bước 3: Tạo đơn hàng và trừ số lượng kho vé (Sử dụng Transaction để an toàn dữ liệu)
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

                // 1. KIỂM TRA TRÙNG GHẾ LẦN CUỐI (Tránh trường hợp 2 người cùng chọn 1 ghế đồng thời)
                var existingSeats = await _context.BookingDetails
                    .Where(d => d.Booking.EventId == eventId && (d.Booking.Status == "Confirmed" || d.Booking.Status == "Pending"))
                    .Select(d => d.SeatNumber)
                    .ToListAsync();

                foreach (var seat in seatArray)
                {
                    if (existingSeats.Contains(seat.Trim()))
                    {
                        return BadRequest($"Ghế {seat} vừa có người khác đặt. Vui lòng quay lại chọn ghế khác.");
                    }
                }

                // 2. KHỞI TẠO ĐƠN HÀNG
                var user = await _userManager.FindByEmailAsync(customerEmail);
                var booking = new Booking
                {
                    EventId = eventId,
                    BookingDate = DateTime.Now, // Sử dụng giờ hệ thống địa phương
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    PhoneNumber = phoneNumber,
                    UserId = user?.Id
                };

                // 3. XỬ LÝ TỪNG GHẾ VÀ TRỪ SỐ LƯỢNG KHO VÉ TƯƠNG ỨNG
                foreach (var seatCode in seatArray)
                {
                    // Logic phân loại hạng vé theo hàng (A,B: VIP | C,D,E: Standard | G,H...: Economy)
                    string row = seatCode.Substring(0, 1).ToUpper();
                    int typeIndex = (row == "A" || row == "B") ? 0 : (row == "C" || row == "D" || row == "E") ? 1 : 2;

                    // Đảm bảo không vượt quá số lượng TicketTypes hiện có của sự kiện
                    var ticketTypesOrdered = @event.TicketTypes.OrderByDescending(t => t.Price).ToList();
                    if (typeIndex >= ticketTypesOrdered.Count) typeIndex = ticketTypesOrdered.Count - 1;

                    var ticketType = ticketTypesOrdered.ElementAt(typeIndex);

                    if (ticketType.Quantity <= 0) return BadRequest($"Hạng vé {ticketType.Name} đã hết chỗ.");

                    booking.BookingDetails.Add(new BookingDetail
                    {
                        TicketTypeId = ticketType.Id,
                        SeatNumber = seatCode.Trim(), //
                        Quantity = 1,
                        UnitPrice = ticketType.Price
                    });

                    // TRỪ SỐ LƯỢNG TRONG HỆ THỐNG (Inventory Management)
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
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý đơn hàng. Vui lòng thử lại.");
            }
        }

        // Bước 4: Xác nhận thanh toán và gửi danh sách QR rời về Email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeBooking(int bookingId, string selectedSeats)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            // Cập nhật trạng thái chính thức
            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // CHUẨN BỊ NỘI DUNG EMAIL GỬI TỪNG VÉ RỜI KÈM MÃ QR
            var seatList = selectedSeats?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            var ticketListHtml = new StringBuilder();

            foreach (var seat in seatList)
            {
                // Tạo mã soát vé ngẫu nhiên duy nhất cho từng vị trí ngồi
                string individualTicketCode = Guid.NewGuid().ToString().ToUpper().Substring(0, 8);
                string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=CHECKIN_{individualTicketCode}";

                ticketListHtml.Append($@"
                    <div style='border: 2px dashed #006D5B; padding: 20px; margin-bottom: 20px; background-color: #ffffff; border-radius: 10px;'>
                        <h2 style='color: #006D5B; margin-top: 0; text-align: center;'>VÉ XEM SỰ KIỆN</h2>
                        <p><b>Sự kiện:</b> {booking.Event?.Title}</p>
                        <p><b>Khách hàng:</b> {booking.CustomerName}</p>
                        <p><b>Vị trí ghế:</b> <span style='font-size: 20px; color: #ce1212;'>{seat.Trim()}</span></p>
                        <p><b>Mã soát vé:</b> {individualTicketCode}</p>
                        <div style='text-align: center; margin-top: 15px;'>
                            <img src='{qrUrl}' width='150' height='150' style='display: block; margin: 0 auto;' alt='QR Code Checkin' />
                        </div>
                        <p style='font-size: 11px; color: #666; margin-top: 10px; text-align: center;'>* Vui lòng đưa mã QR này để nhân viên quét khi vào cổng sự kiện.</p>
                    </div>");
            }

            string subject = $"[Eventus] Xác nhận {seatList.Length} vé thành công - Đơn hàng #{booking.Id}";
            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; color: #333;'>
                    <h3 style='color: #28a745;'>Thanh toán thành công!</h3>
                    <p>Chào <b>{booking.CustomerName}</b>, đơn hàng của bạn đã được xác nhận. Dưới đây là danh sách vé rời của bạn:</p>
                    {ticketListHtml}
                    <div style='background: #f8f9fa; padding: 15px; margin-top: 20px; border-radius: 5px; border: 1px solid #eee;'>
                        <p style='margin:0;'><b>Địa điểm:</b> {booking.Event?.Location}</p>
                        <p style='margin:0;'><b>Thời gian:</b> {booking.Event?.StartDate:dd/MM/yyyy HH:mm}</p>
                    </div>
                    <p style='font-size: 12px; color: #999; text-align: center; margin-top: 20px;'>Cảm ơn bạn đã sử dụng dịch vụ của Eventus.</p>
                </div>";

            await _emailService.SendEmailAsync(booking.CustomerEmail, subject, emailBody);

            return RedirectToAction("BookingSuccess", new { id = booking.Id });
        }

        // Bước 5: Hiển thị trang kết quả đặt vé thành công
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }
    }
}