using EventManagementSystem.Web.Areas.Admin.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
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
            var model = new AdminDashboardViewModel();

            // 1. Thống kê người dùng & sự kiện (Giữ nguyên)
            model.TotalUsers = await _userManager.Users.CountAsync();
            var organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            model.TotalOrganizers = organizers.Count;
            model.TotalActiveEvents = await _context.Events
                .CountAsync(e => e.Status == "Published" && e.IsActive == true);

            // 2. THỐNG KÊ DOANH THU & PHÍ THỰC TẾ (Chỉ tính vé hợp lệ)
            // Tính tổng doanh thu từ các vé CHƯA bị hủy của đơn hàng thành công
            var totalNetRevenue = await _context.BookingDetails
                .Where(d => (d.Booking.Status == "Confirmed" || d.Booking.Status == "Success") && !d.IsCancelled)
                .SumAsync(d => d.UnitPrice * d.Quantity);

            // Phí dịch vụ 10% dựa trên doanh thu thực tế
            model.TotalCommission = totalNetRevenue * 0.1m;

            // Tổng số vé thực tế đã bán (Loại trừ vé đã hủy)
            model.TotalTicketsSold = await _context.BookingDetails
                .Where(d => (d.Booking.Status == "Confirmed" || d.Booking.Status == "Success") && !d.IsCancelled)
                .SumAsync(bd => bd.Quantity);

            // 3. Lấy 5 sự kiện mới cập nhật (Cần gán Id để nút mắt hoạt động)
            model.RecentEvents = await _context.Events
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new RecentEventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    OrganizerName = e.Organizer.FullName ?? "N/A",
                    IsApproved = e.IsActive,
                    CreatedAt = e.CreatedAt
                }).ToListAsync();

            // 4. LẤY 5 GIAO DỊCH PHÍ GẦN NHẤT (Chỉ lấy đơn hàng có vé hợp lệ)
            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            model.RecentTransactions = recentBookings
                .Select(b => {
                    // Tính phí 10% dựa trên các vé chưa bị hủy trong đơn hàng này
                    var netAmount = b.BookingDetails
                        .Where(d => !d.IsCancelled)
                        .Sum(d => d.UnitPrice * d.Quantity);

                    return new
                    {
                        Email = b.CustomerEmail,
                        Fee = netAmount * 0.1m,
                        Event = b.Event?.Title ?? "N/A"
                    };
                })
                .Where(x => x.Fee > 0) // Chỉ hiển thị giao dịch có phát sinh phí
                .Take(5)
                .Select(x => new RecentTransactionViewModel
                {
                    UserEmail = x.Email,
                    Amount = x.Fee,
                    EventTitle = x.Event
                }).ToList();

            return View(model);
        }
    }
}