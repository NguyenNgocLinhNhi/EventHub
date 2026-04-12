using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventManagementSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var allEvents = _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive)
                .OrderBy(e => e.StartDate);

            var spotlight = await allEvents
                .FirstOrDefaultAsync(e => e.LandingPage == "Nova")
                ?? await allEvents.FirstOrDefaultAsync();

            var upcoming = await allEvents
                .Where(e => spotlight == null || e.Id != spotlight.Id)
                .Take(6)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                SpotlightEvent = spotlight,
                UpcomingEvents = upcoming
            };

            return View(viewModel);
        }

        public async Task<IActionResult> About()
        {
            var teamMembers = await _context.TeamMembers
                .Where(m => m.IsVisible)
                .ToListAsync();
            return View(teamMembers);
        }

        public IActionResult Services()
        {
            return View();
        }

        // =========================================================================
        // HÀM EVENTS: X? lý l?c danh sách s? ki?n
        // =========================================================================
        public async Task<IActionResult> Events(int? categoryId, string searchString)
        {
            // 1. Khởi tạo Query lấy sự kiện và bao gồm dữ liệu Category
            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .Where(e => e.IsActive) // Chỉ lấy các sự kiện đang hoạt động
                .AsQueryable();

            // 2. LOGIC: Lọc theo CategoryId (Dành cho các nút Category cụ thể)
            if (categoryId.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);

                var selectedCat = await _context.Categories.FindAsync(categoryId);
                ViewBag.CategoryName = selectedCat?.Name;
                ViewBag.CurrentCategoryId = categoryId;
            }

            // 3. LOGIC: Lọc theo từ khóa tìm kiếm (Mapping Anh-Việt)
            if (!string.IsNullOrEmpty(searchString))
            {
                ViewBag.CurrentFilter = searchString;

                // Chuyển searchString về chữ thường để so sánh
                string lowerSearch = searchString.ToLower();

                // Ánh xạ từ khóa tiếng Anh từ URL sang cụm từ tiếng Việt tương ứng trong SeedData
                string dbSearchTerm = lowerSearch switch
                {
                    "technology" => "Công nghệ",
                    "music" => "Âm nhạc",
                    "food" => "Ẩm thực",
                    "education" => "Giáo dục",
                    "business" => "Kinh doanh",
                    "health" => "Y học",
                    _ => searchString // Giữ nguyên nếu không nằm trong danh sách mapping
                };

                // Tìm kiếm theo Tiêu đề, Vị trí HOẶC Tên danh mục (sử dụng từ khóa đã ánh xạ)
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(dbSearchTerm) ||
                    e.Location.Contains(dbSearchTerm) ||
                    e.Category.Name.Contains(dbSearchTerm) ||
                    e.Title.Contains(searchString) || // Vẫn cho phép tìm bằng từ gốc (ví dụ tìm tên sự kiện "Tech Summit")
                    e.Category.Name.Contains(searchString));

                // Cập nhật tiêu đề hiển thị trên giao diện cho đẹp
                if (string.IsNullOrEmpty(ViewBag.CategoryName))
                {
                    ViewBag.CategoryName = searchString;
                }
            }

            // 4. Sắp xếp và thực thi truy vấn
            var events = await eventsQuery
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> Categories(string searchString)
        {
            var categoriesQuery = _context.Categories
                .Include(c => c.Events)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                categoriesQuery = categoriesQuery
                    .Where(c => c.Name.Contains(searchString));
            }

            var categories = await categoriesQuery
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.CurrentFilter = searchString;

            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Contact(int? eventId)
        {
            var model = new ContactFormModel { EventId = eventId };
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Tự động gán tên và email từ profile vào model
                    model.Name = user.FullName; // Hoặc user.UserName tùy theo DB của bạn
                    model.Email = user.Email;
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormModel model)
        {
            if (ModelState.IsValid)
            {
                var inquiry = new ContactInquiry
                {
                    Name = model.Name,
                    Email = model.Email,
                    Message = model.Message,
                    EventId = model.EventId, // Lưu ID sự kiện vào Database
                    Category = "Attendee",
                    CreatedAt = DateTime.Now,
                    UserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                };

                _context.ContactInquiries.Add(inquiry);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your message has been sent successfully!";
                return RedirectToAction("Contact");
            }
            return View(model);
        }
    }
}