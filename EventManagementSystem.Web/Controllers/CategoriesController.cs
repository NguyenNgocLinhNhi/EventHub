using Microsoft.AspNetCore.Mvc;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;

namespace EventManagementSystem.Web.Controllers
{
    // [Authorize(Roles = "Admin")]
    //AHIHI
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ===== Dependency Injection =====
        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _context.Categories.Add(category);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}
