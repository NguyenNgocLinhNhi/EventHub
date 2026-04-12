using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")] // Chỉ nhà tổ chức mới vào được trang này
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị danh sách các thắc mắc của chính Nhà tổ chức đó
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Lấy danh sách thắc mắc gửi cho Admin (EventId == null) của riêng User này
            var inquiries = await _context.ContactInquiries
                .Where(i => i.Category == "Organizer" && i.UserId == user.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Đánh dấu tất cả các phản hồi từ Admin là "Đã xem" khi NTC vào trang này
            // Điều này sẽ làm sạch chuông thông báo trên Sidebar
            var unreadReplies = inquiries.Where(i => i.IsReplied && !i.IsReadByAttendee).ToList();
            if (unreadReplies.Any())
            {
                foreach (var item in unreadReplies)
                {
                    item.IsReadByAttendee = true;
                }
                await _context.SaveChangesAsync();
            }

            return View(inquiries);
        }
    }
}