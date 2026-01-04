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

        public IActionResult About()
        {
            return View();
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
            // 1. Kh?i t?o Query l?y s? ki?n và bao g?m d? li?u Category
            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .AsQueryable();

            // 2. LOGIC: L?c theo CategoryId (Dành cho các nút Category/Topic)
            if (categoryId.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);

                // L?y tên danh m?c ?? hi?n th? lên tiêu ?? trang (Breadcrumb/Header)
                var selectedCat = await _context.Categories.FindAsync(categoryId);
                ViewBag.CategoryName = selectedCat?.Name;
                ViewBag.CurrentCategoryId = categoryId;
            }

            // 3. LOGIC: L?c theo t? khóa tìm ki?m (Dành cho thanh Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm ki?m theo Tiêu ?? s? ki?n, V? trí HO?C Tên danh m?c (Topic)
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(searchString) ||
                    e.Location.Contains(searchString) ||
                    e.Category.Name.Contains(searchString));

                ViewBag.CurrentFilter = searchString;

                // N?u l?c theo tên Topic trên trang ch?, c?p nh?t tiêu ?? hi?n th?
                if (string.IsNullOrEmpty(ViewBag.CategoryName))
                {
                    ViewBag.CategoryName = searchString;
                }
            }

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

        public IActionResult Contact()
        {
            return View();
        }
    }
}