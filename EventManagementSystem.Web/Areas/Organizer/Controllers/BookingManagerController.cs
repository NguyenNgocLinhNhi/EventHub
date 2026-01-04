using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Web.Data;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class BookingManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BookingManagerController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            return View(bookings);
        }
    }
}
