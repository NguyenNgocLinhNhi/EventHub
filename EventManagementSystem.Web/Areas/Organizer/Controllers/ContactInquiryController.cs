using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class ContactInquiryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactInquiryController(ApplicationDbContext context, IEmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: Organizer/ContactInquiry
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Lấy danh sách thắc mắc, bao gồm thông tin Sự kiện đi kèm
            var inquiries = await _context.ContactInquiries
                .Include(c => c.Event)
                .Where(c => c.Category == "Attendee" || c.Event.OrganizerId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(inquiries);
        }

        // GET: Organizer/ContactInquiry/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var inquiry = await _context.ContactInquiries
                .Include(c => c.Event)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inquiry == null) return NotFound();

            return View(inquiry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyMessage)
        {
            // 1. Tìm thắc mắc
            var inquiry = await _context.ContactInquiries.FindAsync(id);

            if (inquiry == null)
            {
                return NotFound();
            }

            // 2. Kiểm tra nội dung phản hồi không được để trống
            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["ErrorMessage"] = "Reply message cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            try
            {
                // 3. Cập nhật thông tin phản hồi và trạng thái thông báo
                inquiry.ReplyMessage = replyMessage;
                inquiry.IsReplied = true;
                inquiry.RepliedAt = DateTime.Now;

                // Đặt là false để kích hoạt chấm đỏ trên chuông của Attendee
                inquiry.IsReadByAttendee = false;

                _context.Update(inquiry);
                await _context.SaveChangesAsync();

                // 4. Gửi Email thông báo (Thực hiện sau khi lưu DB thành công)
                try
                {
                    string subject = "Response to your inquiry - EventHub";
                    string body = $@"<h3>Hi {inquiry.Name},</h3>
                            <p>We have a response for your inquiry.</p>
                            <p><b>Your message:</b> {inquiry.Message}</p>
                            <p><b>Our Answer:</b> {replyMessage}</p>
                            <p>Best regards,<br/>EventHub Team</p>";

                    await _emailService.SendEmailAsync(inquiry.Email, subject, body);
                    TempData["SuccessMessage"] = "Your response has been recorded and emailed successfully!";
                }
                catch (Exception ex)
                {
                    // Nếu gửi mail lỗi, vẫn báo thành công lưu DB nhưng kèm cảnh báo mail
                    TempData["SuccessMessage"] = "Response saved, but there was an error sending the email.";
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InquiryExists(inquiry.Id))
                {
                    return NotFound();
                }
                else
                {
                    // Log lỗi hoặc xử lý tùy theo yêu cầu dự án
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Hàm kiểm tra sự tồn tại của Inquiry
        private bool InquiryExists(int id)
        {
            return _context.ContactInquiries.Any(e => e.Id == id);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var inquiry = await _context.ContactInquiries.FindAsync(id);
            if (inquiry == null) return Json(new { success = false });

            // Cập nhật trạng thái người tổ chức đã xem
            inquiry.IsReadByOrganizer = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
