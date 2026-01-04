using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // READ: Hiển thị danh sách sự kiện của tổ chức
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var events = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings).ThenInclude(b => b.BookingDetails)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(events);
        }

        // DELETE: Xử lý xóa sự kiện qua AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var @event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);

            if (@event == null)
            {
                return Json(new { success = false, message = "Sự kiện không tồn tại hoặc bạn không có quyền xóa." });
            }

            try
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}