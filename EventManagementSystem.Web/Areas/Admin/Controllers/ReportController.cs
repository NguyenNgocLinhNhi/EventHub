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

        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate)
        {
            var now = DateTime.Now;
            var startOfPeriod = startDate ?? new DateTime(now.Year, now.Month, 1).AddMonths(-6);
            var endOfPeriod = endDate ?? now;

            // 1. Tổng doanh thu thực tế (Chỉ tính vé chưa hủy trong các đơn hàng đã xác nhận)
            ViewBag.TotalRevenue = await _context.BookingDetails
                .Where(d => d.Booking.Status == "Confirmed" && !d.IsCancelled)
                .SumAsync(d => d.UnitPrice * d.Quantity);

            // 2. Tổng số vé thực tế đã bán (Loại bỏ vé đã hủy)
            ViewBag.TicketsSold = await _context.BookingDetails
                .Where(d => d.Booking.Status == "Confirmed" && !d.IsCancelled)
                .SumAsync(d => d.Quantity);

            // 3. Số lượng sự kiện
            ViewBag.TotalEvents = await _context.Events.CountAsync();

            // Doanh thu trung bình mỗi sự kiện (Dựa trên doanh thu thực tế)
            ViewBag.AvgRevenue = ViewBag.TotalEvents > 0 ? (decimal)ViewBag.TotalRevenue / ViewBag.TotalEvents : 0;

            // 4. Dữ liệu thực tế cho biểu đồ xu hướng (Trend) - Tính Net Revenue theo tháng
            var monthlyData = await _context.BookingDetails
                .Where(d => d.Booking.Status == "Confirmed" && !d.IsCancelled && d.Booking.BookingDate >= startOfPeriod && d.Booking.BookingDate <= endOfPeriod)
                .GroupBy(d => new { d.Booking.BookingDate.Year, d.Booking.BookingDate.Month })
                .Select(g => new {
                    Label = g.Key.Month + "/" + g.Key.Year,
                    Value = g.Sum(x => x.UnitPrice * x.Quantity),
                    SortKey = g.Key.Year * 100 + g.Key.Month
                })
                .OrderBy(x => x.SortKey)
                .ToListAsync();

            ViewBag.ChartLabels = monthlyData.Select(x => x.Label).ToList();
            ViewBag.ChartData = monthlyData.Select(x => x.Value).ToList();

            // 5. Danh sách Top Organizers (Tính dựa trên doanh thu thực tế từ các vé hợp lệ)
            var topOrganizers = await _context.Events
                .GroupBy(e => e.Organizer.OrganizationName)
                .Select(g => new {
                    Name = g.Key ?? "System",
                    Count = g.Count(),
                    // Tính tổng tiền từ các vé chưa bị hủy của Organizer này
                    Revenue = _context.BookingDetails
                        .Where(d => d.Booking.Event.Organizer.OrganizationName == g.Key &&
                                    d.Booking.Status == "Confirmed" &&
                                    !d.IsCancelled)
                        .Sum(d => d.UnitPrice * d.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            ViewBag.StartDate = startOfPeriod.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endOfPeriod.ToString("yyyy-MM-dd");

            return View(topOrganizers);
        }
    }
}
