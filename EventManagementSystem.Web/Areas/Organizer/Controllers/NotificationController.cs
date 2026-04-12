using EventManagementSystem.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NotificationController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> MarkRead(string type, int id)
        {
            try
            {
                if (type == "Inquiry")
                {
                    var inquiry = await _context.ContactInquiries.FindAsync(id);
                    if (inquiry != null) inquiry.IsReadByOrganizer = true;
                }
                else if (type == "Booking")
                {
                    var booking = await _context.Bookings.FindAsync(id);
                    if (booking != null) booking.IsReadByOrganizer = true;
                }
                else if (type == "Cancellation")
                {
                    var detail = await _context.BookingDetails.FindAsync(id);
                    if (detail != null) detail.IsReadByOrganizer = true;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }
    }
}