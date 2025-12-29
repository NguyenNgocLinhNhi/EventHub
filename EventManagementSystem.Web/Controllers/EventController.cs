using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities; // Đảm bảo đúng namespace của thực thể Event
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .Include(e => e.Speakers)
                .Include(e => e.Schedules)
                .Include(e => e.Sponsors)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null) return NotFound();

            string landingPageName = eventEntity.LandingPage ?? "Medinova";

            // Đường dẫn trỏ thẳng vào folder sự kiện để lấy file chính
            return View($"~/Views/Event/{landingPageName}/Detail_{landingPageName}.cshtml", eventEntity);
        }
    }
}