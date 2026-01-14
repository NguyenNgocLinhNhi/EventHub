using EventManagementSystem.Web.Areas.Organizer.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Trang chủ công khai (Landing Page Index)
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        // 3. Trang giới thiệu (About)
        public IActionResult About()
        {
            return View();
        }

        // 4. Trang dịch vụ (Services)
        public IActionResult Services()
        {
            return View();
        }

        // 5. Trang sự kiện/Giao diện (Events)
        public IActionResult Events()
        {
            // 1. Xác định đường dẫn: wwwroot/Templates
            var templatesPath = Path.Combine(_webHostEnvironment.WebRootPath, "Templates");
            var templates = new List<EventController.LandingPageTemplateEntity>();

            if (Directory.Exists(templatesPath))
            {
                var directoryInfo = new DirectoryInfo(templatesPath);
                // Lấy tất cả thư mục con
                var folders = directoryInfo.GetDirectories();

                foreach (var folder in folders)
                {
                    // "Con người code" thường sẽ quy ước ảnh đại diện là thumbnail.jpg hoặc preview.png
                    // Nếu project của bạn ảnh nằm sâu trong img/..., ta sẽ ưu tiên tìm file ảnh đầu tiên
                    string imageUrl = GetThumbnail(folder.Name);

                    templates.Add(new EventController.LandingPageTemplateEntity
                    {
                        Id = folder.Name, // ID là tên thư mục (Vd: Charitize, Chefer...)
                        Name = "Template " + folder.Name, // Tên hiển thị tạm thời
                        Description = $"Giao diện chuyên nghiệp phong cách {folder.Name}.",
                        ImageUrl = imageUrl
                    });
                }
            }

            return View(templates);
        }

        // Hàm phụ giúp tìm ảnh đại diện dựa trên project hiện tại của bạn
        private string GetThumbnail(string folderName)
        {
            // Kiểm tra các đường dẫn ảnh mà project của bạn đang dùng trong EventController
            string[] commonPaths = {
                $"/Templates/{folderName}/img/carousel-1.jpg",
                $"/Templates/{folderName}/img/hero-1.jpg",
                $"/Templates/{folderName}/assets/img/hero-bg.jpg",
                $"/Templates/{folderName}/img/hero.jpg",
                $"/Templates/{folderName}/assets/img/hero-img.png"
            };

            foreach (var path in commonPaths)
            {
                if (System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, path.TrimStart('/'))))
                {
                    return path;
                }
            }

            // Nếu không tìm thấy ảnh theo quy ước trên, trả về ảnh mặc định
            return "/img/no-image.jpg";
        }

        // 6. Trang liên hệ (Contact)
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitContact([FromBody] ContactFormModel model)
        {
            // Kiểm tra dữ liệu dựa trên các Attribute trong ContactFormModel
            if (!ModelState.IsValid)
            {
                // Lấy thông báo lỗi đầu tiên để trả về cho người dùng
                var error = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return Json(new { success = false, message = error ?? "Invalid data." });
            }

            // Logic xử lý (Lưu DB hoặc gửi Email)
            // Cần đảm bảo Model có thuộc tính FullName hoặc sửa thành Name cho đồng bộ
            return Json(new { success = true, message = "Thank you " + model.FullName + "! Your request has been received." });
        }

        // 2. Trang Dashboard: Chỉ dành cho nhà tổ chức đã đăng nhập
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userId = user.Id;

            var myEvents = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.BookingDetails)
                .ToListAsync();

            var viewModel = new OrganizerDashboardViewModel
            {
                TotalEvents = myEvents.Count,
                TotalRevenue = myEvents.SelectMany(e => e.Bookings)
                                       .Where(b => b.Status == "Confirmed" || b.Status == "Success")
                                       .Sum(b => b.TotalAmount),
                TotalTickets = myEvents.SelectMany(e => e.Bookings)
                                       .SelectMany(b => b.BookingDetails)
                                       .Sum(d => d.Quantity),
                TotalCustomers = myEvents.SelectMany(e => e.Bookings)
                                         .Select(b => b.CustomerEmail)
                                         .Distinct().Count(),
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