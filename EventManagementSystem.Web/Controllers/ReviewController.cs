using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int eventId, string email)
        {
            // 1. Kiểm tra nếu người dùng đã đăng nhập, ưu tiên lấy Email từ tài khoản
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    email = user.Email;
                }
            }

            // 2. Kiểm tra xem người dùng này đã đánh giá sự kiện này chưa
            var existing = await _context.Reviews.AnyAsync(r => r.EventId == eventId && r.CustomerEmail == email);
            if (existing) return View("AlreadyReviewed");

            // 3. Khởi tạo Model với các thông tin đã biết
            var model = new Review
            {
                EventId = eventId,
                CustomerEmail = email
            };

            // Nếu đã đăng nhập, tự động điền tên từ hồ sơ
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                model.CustomerName = user?.FullName;
            }

            ViewBag.EventTitle = (await _context.Events.FindAsync(eventId))?.Title;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Review review)
        {
            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                _context.Add(review);
                await _context.SaveChangesAsync();
                return View("Success");
            }
            return View(review);
        }

        
    }
}
