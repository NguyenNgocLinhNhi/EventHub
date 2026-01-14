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

            // 1. Thống kê người dùng
            model.TotalUsers = await _userManager.Users.CountAsync();

            // Lấy danh sách Organizer dựa trên Role "Organizer"
            var organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            model.TotalOrganizers = organizers.Count;

            // 2. Thống kê sự kiện (Sử dụng thuộc tính Status trong Event.cs)
            model.TotalActiveEvents = await _context.Events
                .CountAsync(e => e.Status == "Published" && e.IsActive == true);

            // 3. Thống kê vé và doanh thu (Sử dụng thuộc tính TotalAmount trong Booking.cs)
            // Tính tổng doanh thu từ tất cả các Booking có trạng thái "Confirmed" hoặc "Success"
            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                .SumAsync(b => b.TotalAmount);

            // Phí commission hệ thống (ví dụ 10% doanh thu)
            model.TotalCommission = totalRevenue * 0.1m;

            // Tổng số vé đã bán (Sum Quantity từ BookingDetails)
            model.TotalTicketsSold = await _context.BookingDetails.SumAsync(bd => bd.Quantity);

            model.TotalSystemViews = 15420; // Giả lập dữ liệu traffic

            // 4. Lấy 5 sự kiện mới cập nhật (Sử dụng quan hệ Virtual Organizer trong Event.cs)
            model.RecentEvents = await _context.Events
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new RecentEventViewModel
                {
                    Title = e.Title,
                    OrganizerName = e.Organizer.FullName ?? "N/A",
                    IsApproved = e.IsActive, // Dùng IsActive để hiển thị trạng thái
                    CreatedAt = e.CreatedAt
                }).ToListAsync();

            // 5. Lấy 5 giao dịch gần nhất (Dữ liệu từ Booking.cs)
            model.RecentTransactions = await _context.Bookings
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .Select(b => new RecentTransactionViewModel
                {
                    UserEmail = b.CustomerEmail,
                    Amount = b.TotalAmount,
                    EventTitle = b.Event.Title
                }).ToListAsync();

            return View(model);
        }
    }
}