using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyInquiries()
        {
            var userId = _userManager.GetUserId(User);
            var myInquiries = await _context.ContactInquiries
                .Include(i => i.Event)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Khi vào trang này, tự động đánh dấu tất cả là đã đọc để mất chấm đỏ
            var unread = myInquiries.Where(i => !i.IsReadByAttendee).ToList();
            if (unread.Any())
            {
                foreach (var item in unread) { item.IsReadByAttendee = true; }
                await _context.SaveChangesAsync();
            }

            return View(myInquiries);
        }
    }
}
