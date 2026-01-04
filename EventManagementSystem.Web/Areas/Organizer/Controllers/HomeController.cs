using EventManagementSystem.Web.Areas.Organizer.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Trang chủ công khai: Ai cũng có thể vào xem giới thiệu
        public IActionResult Index()
        {
            // Nếu đã đăng nhập, tự động chuyển hướng vào Dashboard số liệu
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        // 2. Trang Dashboard: Chỉ dành cho tài khoản công ty đã đăng nhập
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // Lấy User hiện tại kèm theo thông tin OrganizationName
            var user = await _userManager.GetUserAsync(User);

            // Kiểm tra nếu không phải tài khoản tổ chức thì không cho xem số liệu
            if (user == null || string.IsNullOrEmpty(user.OrganizationName))
            {
                return Forbid(); // Trả về lỗi không có quyền truy cập
            }

            var userId = user.Id;

            // Lấy danh sách sự kiện kèm đơn hàng (Bookings) của đúng ID này
            var myEvents = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.BookingDetails)
                .ToListAsync();

            // Tính toán số liệu thực tế dựa trên Database
            var viewModel = new OrganizerDashboardViewModel
            {
                TotalEvents = myEvents.Count,

                // Tổng doanh thu thực tế từ đơn hàng thành công
                TotalRevenue = myEvents.SelectMany(e => e.Bookings)
                                       .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                                       .Sum(b => b.TotalAmount),

                // Tổng số vé thực tế đã bán
                TotalTickets = myEvents.SelectMany(e => e.Bookings)
                                       .SelectMany(b => b.BookingDetails)
                                       .Sum(d => d.Quantity),

                // Tổng số khách hàng thực tế
                TotalCustomers = myEvents.SelectMany(e => e.Bookings)
                                         .Select(b => b.CustomerEmail)
                                         .Distinct().Count(),

                // Danh sách đơn hàng gần đây nhất
                RecentBookings = await _context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.Event.OrganizerId == userId)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5).ToListAsync()
            };

            return View(viewModel);
        }
    }
}