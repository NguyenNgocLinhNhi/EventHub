using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Models.Entities; // Đảm bảo có namespace này
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ===================== READ: DANH SÁCH KHÁCH HÀNG =====================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Lấy tất cả đơn hàng thuộc về các sự kiện của Organizer này
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .Where(b => b.Event.OrganizerId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // ===================== AJAX: ĐIỂM DANH (CHECK-IN) =====================
        [HttpPost]
        public async Task<IActionResult> ToggleCheckIn(int id)
        {
            // Tìm đơn hàng (Booking) tương ứng
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return Json(new { success = false });

            // Đảo ngược trạng thái IsCheckedIn (Đã thêm trong Model Booking trước đó)
            booking.IsCheckedIn = !booking.IsCheckedIn;
            booking.CheckedInAt = booking.IsCheckedIn ? DateTime.Now : null;

            await _context.SaveChangesAsync();
            return Json(new { success = true, isCheckedIn = booking.IsCheckedIn });
        }

        // ===================== UPDATE: TRẠNG THÁI ĐƠN HÀNG =====================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return Json(new { success = false });

            booking.Status = status; // Confirmed, Cancelled, Pending
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===================== DELETE: XÓA HỒ SƠ =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // READ: Chi tiết vé (Sử dụng cho Modal)
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();
            return PartialView("_BookingDetails", booking);
        }

        // Action lấy danh sách khách hàng đã từng mua vé
        public async Task<IActionResult> Customers()
        {
            var userId = _userManager.GetUserId(User);

            // 1. Lấy toàn bộ danh sách đơn hàng thuộc sự kiện của Organizer này
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Event.OrganizerId == userId)
                .ToListAsync();

            // 2. Nhóm theo Email để tính toán chi tiêu và phân hạng
            var customers = bookings
                .GroupBy(b => b.CustomerEmail)
                .Select(g => {
                    var totalSpent = g.Sum(b => b.TotalAmount); // Tổng chi tiêu

                    return new
                    {
                        IntId = g.First().Id, // Lấy ID của đơn hàng đầu tiên để làm mã KH
                        CustomerName = g.First().CustomerName,
                        CustomerEmail = g.Key,
                        // Ưu tiên lấy số điện thoại từ Booking, nếu không có dùng PhoneNumber
                        CustomerPhone = g.First().PhoneNumber ?? g.First().PhoneNumber,
                        TotalSpent = totalSpent,

                        // Ràng buộc phân hạng khách hàng
                        Rank = totalSpent switch
                        {
                            >= 10000000 => "Diamond", // Trên 10 triệu
                            >= 5000000 => "Gold",    // Từ 5 triệu đến dưới 10 triệu
                            >= 1000000 => "Silver",  // Từ 1 triệu đến dưới 5 triệu
                            _ => "Bronze"            // Dưới 1 triệu
                        }
                    };
                })
                .OrderByDescending(c => c.TotalSpent) // Sắp xếp khách chi tiêu nhiều nhất lên đầu
                .ToList();

            return View(customers);
        }

      /*  [HttpPost]
        public async Task<IActionResult> SendBulkEmail(string subject, string body)
        {
            var userId = _userManager.GetUserId(User);

            // Lấy toàn bộ thông tin khách hàng để cá nhân hóa nội dung
            var customers = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Event.OrganizerId == userId)
                .Select(b => new { b.CustomerName, b.CustomerEmail, b.TotalAmount }) // Giả sử tính tổng ở đây hoặc dùng Rank
                .Distinct()
                .ToListAsync();

            if (!customers.Any()) return Json(new { success = false, message = "Không có khách hàng nào." });

            int successCount = 0;

            // 2. Thiết lập kết nối SMTP
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("nhinnl22@uef.edu.vn", "jlhbjmhrtritadow");

                    foreach (var customer in customers)
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress("Ban Tổ Chức", "nhinnl22@uef.edu.vn"));
                        message.To.Add(new MailboxAddress(customer.CustomerName, customer.CustomerEmail));
                        message.Subject = subject;

                        // Tạo nội dung HTML chuyên nghiệp
                        var bodyBuilder = new BodyBuilder
                        {
                            HtmlBody = $@"
                <div style='background-color: #f4f4f4; padding: 40px 0; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                    <div style='max-width: 600px; margin: auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.1);'>
                        <div style='background: linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%); padding: 30px; text-align: center;'>
                            <h1 style='color: white; margin: 0; font-size: 24px; letter-spacing: 1px;'>THÔNG BÁO ƯU ĐÃI ĐẶC QUYỀN</h1>
                        </div>
                        <div style='padding: 30px; line-height: 1.6; color: #333;'>
                            <p style='font-size: 18px;'>Xin chào <strong>{customer.CustomerName}</strong>,</p>
                            <p>Lời đầu tiên, chúng tôi xin cảm ơn sự đồng hành tuyệt vời của bạn với tư cách là một khách hàng thân thiết.</p>
                            <div style='background: #fef2f2; border-left: 5px solid #ef4444; padding: 20px; margin: 20px 0;'>
                                <p style='margin: 0; font-style: italic; color: #b91c1c;'>""{body.Replace("\n", "<br/>")}""</p>
                            </div>
                            <p>Đừng bỏ lỡ những sự kiện sắp tới của chúng tôi với nhiều đặc quyền chỉ dành riêng cho bạn.</p>
                            <div style='text-align: center; margin-top: 30px;'>
                                <a href='https://yourwebsite.com' style='background: #ef4444; color: white; padding: 15px 30px; text-decoration: none; border-radius: 50px; font-weight: bold; display: inline-block;'>XEM SỰ KIỆN NGAY</a>
                            </div>
                        </div>
                        <div style='background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px;'>
                            <p>© 2026 EventHub System. Cám ơn bạn đã là khách hàng Diamond của chúng tôi.</p>
                        </div>
                    </div>
                </div>"
                        };
                        message.Body = bodyBuilder.ToMessageBody();
                        await client.SendAsync(message);
                        successCount++;
                    }
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = true, count = successCount });
        }
*/
        [HttpPost]
        public async Task<IActionResult> SendBulkEmail(string subject, string body)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Lấy danh sách Email và Tên duy nhất của toàn bộ khách hàng
            var customers = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Event.OrganizerId == userId)
                .Select(b => new { b.CustomerName, b.CustomerEmail })
                .Distinct()
                .ToListAsync();

            if (!customers.Any()) return Json(new { success = false, message = "Không có khách hàng nào." });

            int successCount = 0;

            // 2. Thiết lập kết nối SMTP
            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("nhinnl22@uef.edu.vn", "jlhbjmhrtritadow");

                    foreach (var customer in customers)
                    {
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress("Ban Tổ Chức", "nhinnl22@uef.edu.vn"));
                        message.To.Add(new MailboxAddress(customer.CustomerName, customer.CustomerEmail));
                        message.Subject = subject;

                        // Tạo nội dung HTML chuyên nghiệp
                        var bodyBuilder = new BodyBuilder
                        {
                            HtmlBody = $@"
                        <div style='font-family: Segoe UI, Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                            <h2 style='color: #0d6efd; border-bottom: 2px solid #0d6efd; padding-bottom: 10px;'>THÔNG BÁO SỰ KIỆN MỚI</h2>
                            <p>Xin chào <strong>{customer.CustomerName}</strong>,</p>
                            <div style='background: #f8f9fa; padding: 15px; border-left: 4px solid #0d6efd; margin: 20px 0;'>
                                {body.Replace("\n", "<br/>")}
                            </div>
                            <p>Trân trọng,<br/>Ban tổ chức EventHub</p>
                        </div>"
                        };
                        message.Body = bodyBuilder.ToMessageBody();

                        await client.SendAsync(message);
                        successCount++;
                    }
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            return Json(new { success = true, count = successCount });
        }

        // Action lấy lịch sử mua vé cho Modal
        public async Task<IActionResult> GetCustomerHistory(string email)
        {
            var userId = _userManager.GetUserId(User);

            var history = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .Where(b => b.Event.OrganizerId == userId && b.CustomerEmail == email)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return PartialView("_CustomerHistoryTable", history);
        }

        [HttpPost]
        public async Task<IActionResult> EditCustomer(string email, string newName, string newPhone)
        {
            // Tìm tất cả các đơn hàng của khách hàng này để cập nhật thông tin đồng bộ
            var bookings = await _context.Bookings
                .Where(b => b.CustomerEmail == email)
                .ToListAsync();

            if (!bookings.Any()) return Json(new { success = false });

            foreach (var b in bookings)
            {
                b.CustomerName = newName;
                b.PhoneNumber = newPhone;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Action lấy thông tin chi tiết khách hàng và các sự kiện đã tham gia (cho icon Bút chì)
        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(string email)
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .Where(b => b.Event.OrganizerId == userId && b.CustomerEmail == email)
                .ToListAsync();

            // Tính tổng chi tiêu của khách hàng này cho các sự kiện của Organizer
            var totalSpent = bookings.Sum(b => b.TotalAmount);

            var eventGroups = bookings
                .GroupBy(b => new { b.Event.Title, b.Event.StartDate })
                .Select(g => new {
                    EventTitle = g.Key.Title,
                    EventDate = g.Key.StartDate,
                    Tickets = g.SelectMany(b => b.BookingDetails).Select(d => new {
                        TicketName = d.TicketType?.Name ?? "Vé không xác định",
                        Quantity = d.Quantity,
                        Price = d.UnitPrice
                    }).ToList()
                })
                .OrderByDescending(x => x.EventDate)
                .ToList();

            return Json(new
            {
                totalSpent = totalSpent,
                events = eventGroups
            });
        }

        [HttpPost]
        [Authorize(Roles = "Organizer,Staff")]
        public async Task<IActionResult> ProcessCheckIn(string ticketCode)
        {
            if (string.IsNullOrEmpty(ticketCode))
                return Json(new { success = false, message = "Mã vé trống!" });

            // 1. Tìm chi tiết vé
            var ticket = await _context.BookingDetails
                .Include(d => d.Booking)
                .Include(d => d.TicketType) // Include thêm để hiện loại vé nếu cần
                .FirstOrDefaultAsync(d => d.TicketCode == ticketCode);

            // 2. Kiểm tra tồn tại
            if (ticket == null)
                return Json(new { success = false, message = "Vé không tồn tại trên hệ thống!" });

            // 3. Kiểm tra trạng thái đơn hàng (Chỉ cho phép check-in nếu đã thanh toán)
            if (ticket.Booking.Status != "Confirmed")
                return Json(new { success = false, message = "Đơn hàng này chưa được xác nhận thanh toán!" });

            // 4. Kiểm tra đã check-in chưa
            if (ticket.IsCheckedIn)
                return Json(new { success = false, message = $"Vé này đã check-in lúc {ticket.CheckInTime:HH:mm dd/MM}!" });

            // 5. Cập nhật trạng thái
            ticket.IsCheckedIn = true;
            ticket.CheckInTime = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();

                // Trả về thêm thông tin để Organizer dễ đối soát
                return Json(new
                {
                    success = true,
                    message = "Check-in thành công!",
                    customer = ticket.Booking.CustomerName ?? ticket.Booking.CustomerEmail,
                    seat = ticket.SeatNumber
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống khi lưu dữ liệu!" });
            }
        }

       /* public async Task<JsonResult> CheckIn(string code, int eventId)
        {
            try
            {
                // Sử dụng TicketCode và EventId để tìm kiếm
                var ticket = await _context.BookingDetails
                    .Include(d => d.Booking)
                        .ThenInclude(b => b.User)
                    .Include(d => d.TicketType)
                    .FirstOrDefaultAsync(d => d.TicketCode == code && d.Booking.EventId == eventId);

                if (ticket == null)
                {
                    return Json(new { success = false, message = "Vé không tồn tại hoặc không thuộc sự kiện này!" });
                }

                // Kiểm tra Status từ Model Booking
                if (ticket.Booking.Status != "Confirmed")
                {
                    return Json(new { success = false, message = "Đơn hàng chưa được xác nhận thanh toán!" });
                }

                // Kiểm tra IsCheckedIn từ Model BookingDetail
                if (ticket.IsCheckedIn)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Vé đã dùng lúc {ticket.CheckInTime?.ToString("HH:mm")}", // Khớp trường CheckInTime
                        customer = ticket.Booking.CustomerName // Khớp trường CustomerName
                    });
                }

                // Cập nhật trạng thái dựa trên thuộc tính Model
                ticket.IsCheckedIn = true;
                ticket.CheckInTime = DateTime.Now; // Đồng bộ với thuộc tính CheckInTime trong BookingDetail.cs

                _context.Update(ticket);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Check-in thành công!",
                    customer = ticket.Booking.CustomerName,
                    seat = ticket.SeatNumber // Khớp trường SeatNumber
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi kết nối máy chủ!" });
            }
        }*/

        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = "Upcoming"; // Cập nhật trường Status
            ev.IsActive = true;     // Cập nhật trường IsActive

            await _context.SaveChangesAsync();
            return Ok();
        }

        // 1. Trả về giao diện Camera
        public IActionResult Scan(int id) // id này là EventId lấy từ URL
        {
            if (id <= 0) return NotFound();

            // Truyền ID này vào View làm Model để JS sử dụng
            return View(id);
        }

        // 2. API xử lý quét mã và tự động lưu vào DB
        [HttpPost]
        public async Task<JsonResult> CheckIn(string code, int eventId)
        {
            // Tìm chi tiết vé và bao gồm thông tin Booking để lấy EventId
            var ticket = await _context.BookingDetails
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => d.TicketCode == code);

            if (ticket == null)
                return Json(new { success = false, message = "Mã vé không tồn tại!" });

            // SO SÁNH ID: Kiểm tra vé có thuộc đúng sự kiện đang quét không
            if (ticket.Booking.EventId != eventId)
            {
                return Json(new
                {
                    success = false,
                    message = $"Vé này thuộc sự kiện #{ticket.Booking.EventId}. Bạn đang quét cho sự kiện #{eventId}!"
                });
            }

            if (ticket.IsCheckedIn)
                return Json(new { success = false, message = "Vé đã được sử dụng (Check-in rồi)!" });

            // Lưu dữ liệu nếu khớp
            ticket.IsCheckedIn = true;
            ticket.CheckInTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Check-in thành công!", customer = ticket.Booking.CustomerName });
        }



    }
}