using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // DI thay cho new DbContext()
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
                SpotlightEvent = spotlight,      // nullable OK
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

        public async Task<IActionResult> Events(int? categoryId, string searchString)
        {
            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);

                var selectedCat = await _context.Categories.FindAsync(categoryId);
                ViewBag.CategoryName = selectedCat?.Name;
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Title.Contains(searchString) ||
                    e.Location.Contains(searchString));
            }

            var events = await eventsQuery
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            ViewBag.CurrentFilter = searchString;

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
