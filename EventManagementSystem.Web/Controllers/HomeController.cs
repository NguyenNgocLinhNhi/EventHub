using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // HÀM HỖ TRỢ: Gán Slug vào ViewBag để dùng chung cho Layout Menu
        private void SetOrgContext(string slug)
        {
            ViewBag.OrgSlug = slug;
        }

        [Route("")]
        [Route("org/{slug?}")]
        public async Task<IActionResult> Index(string slug)
        {
            SetOrgContext(slug);
            var eventsQuery = _context.Events.Include(e => e.Category).Where(e => e.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(slug))
            {
                // 1. Tìm chính xác User sở hữu Slug này
                var organizer = await _context.Users.FirstOrDefaultAsync(x => x.Slug == slug);

                if (organizer == null) return NotFound();

                // 2. Lọc sự kiện THEO ID của User vừa tìm thấy
                eventsQuery = eventsQuery.Where(e => e.OrganizerId == organizer.Id);

                // Debug thử xem ID tìm được là ai (Xem ở cửa sổ Output)
                System.Diagnostics.Debug.WriteLine($"Slug: {slug} belongs to ID: {organizer.Id}");
            }

            // 3. Thực thi lấy dữ liệu từ Query đã lọc
            // Nếu là Org B chưa có sự kiện, allEvents sẽ là một danh sách trống.
            var allEvents = eventsQuery.OrderBy(e => e.StartDate);

            var spotlight = await allEvents.FirstOrDefaultAsync(); // Sẽ là null nếu B chưa có event

            var upcoming = await allEvents
                .Skip(spotlight == null ? 0 : 1) // Nếu ko có spotlight thì ko skip
                .Take(6)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                SpotlightEvent = spotlight,
                UpcomingEvents = upcoming
            };

            return View(viewModel);
        }

        [Route("org/{slug}/about")]
        [Route("about")]
        public IActionResult About(string slug)
        {
            SetOrgContext(slug);
            return View();
        }

        [Route("org/{slug}/services")]
        [Route("services")]
        public IActionResult Services(string slug)
        {
            SetOrgContext(slug);
            return View();
        }

        // =========================================================================
        // HÀM EVENTS: Đã thêm lọc theo Slug
        // =========================================================================
        [Route("org/{slug}/events")]
        [Route("events")]
        public async Task<IActionResult> Events(string slug, int? categoryId, string searchString)
        {
            SetOrgContext(slug);

            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .Where(e => e.IsActive)
                .AsQueryable();

            // Lọc theo Organizer nếu có slug trên URL
            if (!string.IsNullOrEmpty(slug))
            {
                var organizer = await _context.Users.FirstOrDefaultAsync(x => x.Slug == slug);
                if (organizer != null)
                {
                    eventsQuery = eventsQuery.Where(e => e.OrganizerId == organizer.Id);
                }
            }

            // Lọc theo Category
            if (categoryId.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);
                var selectedCat = await _context.Categories.FindAsync(categoryId);
                ViewBag.CategoryName = selectedCat?.Name;
                ViewBag.CurrentCategoryId = categoryId;
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                ViewBag.CurrentFilter = searchString;
                string lowerSearch = searchString.ToLower();
                string dbSearchTerm = lowerSearch switch
                {
                    "technology" => "Công nghệ",
                    "music" => "Âm nhạc",
                    "food" => "Ẩm thực",
                    "education" => "Giáo dục",
                    "business" => "Kinh doanh",
                    "health" => "Y học",
                    _ => searchString
                };

                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(dbSearchTerm) ||
                    e.Location.Contains(dbSearchTerm) ||
                    e.Category.Name.Contains(dbSearchTerm) ||
                    e.Title.Contains(searchString));
            }

            var events = await eventsQuery
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(events);
        }

        [Route("org/{slug}/categories")]
        [Route("categories")]
        public async Task<IActionResult> Categories(string slug, string searchString)
        {
            SetOrgContext(slug);

            var categoriesQuery = _context.Categories
                .Include(c => c.Events)
                .AsQueryable();

            // Nếu muốn chỉ hiện category mà Org đó có sự kiện (tùy chọn)
            if (!string.IsNullOrEmpty(slug))
            {
                var organizer = await _context.Users.FirstOrDefaultAsync(x => x.Slug == slug);
                if (organizer != null)
                {
                    // Chỉ lấy những category mà có ít nhất 1 event của Org này
                    categoriesQuery = categoriesQuery.Where(c => c.Events.Any(e => e.OrganizerId == organizer.Id));
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(searchString));
            }

            var categories = await categoriesQuery.OrderBy(c => c.Name).ToListAsync();
            ViewBag.CurrentFilter = searchString;

            return View(categories);
        }

        [HttpGet]
        [Route("org/{slug}/contact")]
        [Route("contact")]
        public IActionResult Contact(string slug)
        {
            SetOrgContext(slug);
            return View();
        }

        [HttpPost]
        [Route("org/{slug}/contact")]
        [Route("contact")]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string slug, ContactFormModel model)
        {
            SetOrgContext(slug);
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Your message has been sent successfully!";
                return RedirectToAction("Contact", new { slug = slug });
            }
            return View(model);
        }
    }
}