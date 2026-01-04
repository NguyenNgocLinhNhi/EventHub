using EventManagementSystem.Web.Areas.Organizer.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Nạp thêm TicketTypes để biết tổng số vé ban đầu của từng sự kiện
            var myEvents = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.BookingDetails)
                .ToListAsync();

            var viewModel = new OrganizerDashboardViewModel
            {
                TotalEvents = myEvents.Count,

                // Tổng doanh thu từ các đơn hàng thành công
                TotalRevenue = myEvents.SelectMany(e => e.Bookings)
                                       .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                                       .Sum(b => b.TotalAmount),

                // Tổng số vé đã bán thực tế
                TotalTickets = myEvents.SelectMany(e => e.Bookings)
                                       .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                                       .SelectMany(b => b.BookingDetails)
                                       .Sum(d => d.Quantity),

                TotalCustomers = myEvents.SelectMany(e => e.Bookings)
                                         .Select(b => b.CustomerEmail)
                                         .Distinct().Count(),

                // Lấy các sự kiện gần đây nhất
                RecentEvents = myEvents.OrderByDescending(e => e.StartDate).Take(5).ToList(),

                RecentBookings = await _context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.Event.OrganizerId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}