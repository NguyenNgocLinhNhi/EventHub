using EventManagementSystem.Web.Areas.Admin.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services; // Đảm bảo đã nhúng EmailService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrganizerManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // Service gửi mail

        public OrganizerManagerController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        // CHỈ lấy danh sách Nhà tổ chức (Organizer)
        public async Task<IActionResult> Index()
        {
            var organizers = await _userManager.GetUsersInRoleAsync("Organizer");

            var viewModel = organizers.Select(u => new OrganizerManagementViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Region = u.Region ?? "Unspecified",
                // Đếm số sự kiện mà Organizer này đã tạo
                TotalEvents = _context.Events.Count(e => e.OrganizerId == u.Id),
                // Kiểm tra trạng thái Lockout thực tế của Identity
                IsActive = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now,
                CurrentRole = "Organizer"
            }).OrderBy(o => o.FullName).ToList();

            return View(viewModel);
        }

        // Xem lịch sử sử dụng/mua template của tổ chức
        public async Task<IActionResult> PurchaseHistory(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Truy vấn kết hợp bảng Event và LandingPageTemplate
            var history = await _context.Events
                .Where(e => e.OrganizerId == id)
                .Join(_context.LandingPageTemplates,
                    e => e.LandingPage, // LandingPage trong Event là String, khớp với Id trong LandingPageTemplate
                    t => t.Id,
                    (e, t) => new {
                        EventId = e.Id, // Lấy ID để làm nút chi tiết
                        EventTitle = e.Title,
                        TemplateName = t.Name,
                        Date = e.CreatedAt,
                        Status = e.Status
                    })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            ViewBag.OrganizerName = user.FullName;
            return View(history);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            bool isCurrentlyActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.Now;
            string subject;
            string body;

            if (isCurrentlyActive)
            {
                // Thực hiện KHÓA tài khoản
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
                subject = "Thông báo: Tài khoản của bạn đã bị tạm khóa";
                body = $"Chào {user.FullName}, tài khoản nhà tổ chức của bạn trên hệ thống Eventus đã bị tạm khóa bởi quản trị viên. Vui lòng liên hệ hỗ trợ để biết thêm chi tiết.";
            }
            else
            {
                // Thực hiện MỞ KHÓA tài khoản
                await _userManager.SetLockoutEndDateAsync(user, null);
                subject = "Thông báo: Tài khoản của bạn đã được kích hoạt lại";
                body = $"Chào {user.FullName}, tài khoản nhà tổ chức của bạn đã được mở khóa. Bạn có thể đăng nhập và quản lý sự kiện ngay bây giờ.";
            }

            // Gửi email thông báo tự động
            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            return Json(new { success = true, newState = !isCurrentlyActive });
        }

        [HttpPost]
        public async Task<IActionResult> SendBroadcast(string subject, string message)
        {
            try
            {
                // Lấy danh sách email của tất cả Organizer
                var organizers = await _userManager.GetUsersInRoleAsync("Organizer");
                var emails = organizers.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

                if (emails.Any())
                {
                    // Duyệt qua từng email để gửi thông báo
                    foreach (var email in emails)
                    {
                        // Sử dụng IEmailServices đã tiêm vào constructor ở các bước trước
                        await _emailService.SendEmailAsync(email!, subject, message);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}