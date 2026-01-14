using EventManagementSystem.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Revenue()
        {
            var now = DateTime.Now;
            var startOfPeriod = new DateTime(now.Year, now.Month, 1).AddMonths(-6);

            // 1. Dữ liệu thực tế cho 4 thẻ thống kê
            ViewBag.TotalRevenue = await _context.Bookings.Where(b => b.Status == "Confirmed").SumAsync(b => b.TotalAmount);
            ViewBag.TicketsSold = await _context.BookingDetails.CountAsync(d => d.Booking.Status == "Confirmed");
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.AvgRevenue = ViewBag.TotalEvents > 0 ? (decimal)ViewBag.TotalRevenue / ViewBag.TotalEvents : 0;

            // 2. Dữ liệu thực tế cho biểu đồ xu hướng (Trend)
            var monthlyData = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.BookingDate >= startOfPeriod)
                .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                .Select(g => new {
                    Label = g.Key.Month + "/" + g.Key.Year,
                    Value = g.Sum(x => x.TotalAmount)
                }).ToListAsync();

            ViewBag.ChartLabels = monthlyData.Select(x => x.Label).ToList();
            ViewBag.ChartData = monthlyData.Select(x => x.Value).ToList();

            // 3. Danh sách Top Organizers
            var topOrganizers = await _context.Events
                .GroupBy(e => e.Organizer.OrganizationName)
                .Select(g => new {
                    Name = g.Key ?? "System",
                    Count = g.Count(),
                    Revenue = _context.Bookings.Where(b => g.Select(x => x.Id).Contains(b.EventId) && b.Status == "Confirmed").Sum(b => b.TotalAmount)
                })
                .OrderByDescending(x => x.Revenue).Take(5).ToListAsync();

            return View(topOrganizers);
        }
    }
}
