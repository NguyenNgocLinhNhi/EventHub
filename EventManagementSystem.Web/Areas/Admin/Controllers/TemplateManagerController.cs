using EventManagementSystem.Web.Areas.Admin.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static EventManagementSystem.Web.Areas.Organizer.Controllers.EventController;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TemplateManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TemplateManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang danh sách chính
        public async Task<IActionResult> Index()
        {
            // Luôn lấy dữ liệu mới nhất từ DB để đảm bảo UsageCount chính xác
            var viewModel = await _context.LandingPageTemplates
                .Select(t => new TemplateManagementViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    PreviewImageUrl = t.PreviewImageUrl,
                    IsActive = t.IsActive,
                    // Truy vấn đếm trực tiếp từ bảng Events
                    UsageCount = _context.Events.Count(e => e.LandingPage == t.Id)
                }).ToListAsync();

            return View(viewModel);
        }

        // 2. AJAX: Chi tiết sử dụng (Trả về cho Modal)
        public async Task<IActionResult> Details(string id)
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.LandingPage == id)
                .Select(e => new {
                    Id = e.Id,
                    Title = e.Title,
                    OrganizerName = e.Organizer.FullName ?? "N/A",
                    StartDate = e.StartDate
                })
                .ToListAsync();

            return PartialView("_TemplateUsagePartial", events);
        }

        // 3. GET: Thêm mới
        public IActionResult Create() => View();

        // 4. POST: Thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LandingPageTemplate template)
        {
            if (ModelState.IsValid)
            {
                _context.Add(template);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(template);
        }

        // 5. GET: Chỉnh sửa
        public async Task<IActionResult> Edit(string id)
        {
            var template = await _context.LandingPageTemplates.FindAsync(id);
            if (template == null) return NotFound();
            return View(template);
        }

        // 6. POST: Chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, LandingPageTemplate template, IFormFile? imageFile)
        {
            if (id != template.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý tải file nếu Admin có chọn file mới
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Định nghĩa thư mục lưu trữ
                        string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/templates");
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        // Tạo tên file duy nhất
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string filePath = Path.Combine(uploadDir, fileName);

                        // Lưu file vào server
                        using (var varStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(varStream);
                        }

                        // Cập nhật đường dẫn mới vào database (Xóa ảnh cũ nếu cần thiết)
                        template.PreviewImageUrl = "/img/templates/" + fileName;
                    }

                    _context.Update(template);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu: " + ex.Message);
                }
            }
            return View(template);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var template = await _context.LandingPageTemplates.FindAsync(id);
            if (template == null) return Json(new { success = false, message = "Không tìm thấy mẫu." });

            // Kiểm tra xem có sự kiện nào đang sử dụng không
            var usageCount = await _context.Events.CountAsync(e => e.LandingPage == id);
            if (usageCount > 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"Không thể xóa! Hiện có {usageCount} sự kiện đang sử dụng mẫu này."
                });
            }

            _context.LandingPageTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> Usage(string id)
        {
            var template = await _context.LandingPageTemplates.FindAsync(id);
            if (template == null) return NotFound();

            // Lấy danh sách sự kiện kèm thông tin nhà tổ chức
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.LandingPage == id)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            ViewBag.TemplateName = template.Name;
            ViewBag.TemplateId = template.Id;

            return View(events);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var template = await _context.LandingPageTemplates.FindAsync(id);
            if (template == null) return Json(new { success = false, message = "Không tìm thấy mẫu." });

            // Đảo ngược trạng thái hoạt động
            template.IsActive = !template.IsActive;

            _context.Update(template);
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = template.IsActive });
        }

        // Action xử lý việc xem Demo giao diện
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            // 1. Tìm thông tin Template trong cơ sở dữ liệu
            var template = await _context.LandingPageTemplates.FindAsync(id);

            if (template == null)
            {
                return NotFound("Mẫu giao diện không tồn tại trong hệ thống.");
            }


            // 3. Chuyển hướng trực tiếp đến thư mục chứa file tĩnh index.html
            // Đường dẫn này khớp với luồng logic ~/Templates/@item.Id/index.html mà bạn yêu cầu
            string demoPath = $"/Templates/{id}/index.html";

            return Redirect(demoPath);
        }
    }
}