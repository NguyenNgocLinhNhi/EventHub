using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        // ===================== READ: DANH SÁCH =====================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings).ThenInclude(b => b.BookingDetails)
                .Where(e => e.OrganizerId == userId)
                .ToListAsync();

            return View(events);
        }


        // ===================== READ: CHI TIẾT =====================
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var @event = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings!).ThenInclude(b => b.BookingDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);

            if (@event == null) return NotFound();

            // LẤY DỮ LIỆU ĐÁNH GIÁ
            var reviews = await _context.Reviews
                .Where(r => r.EventId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Tính toán các chỉ số để hiển thị trên View
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.ReviewsList = reviews; // Truyền danh sách đánh giá xuống View

            return View(@event);
        }
        // ===================== CREATE: TẠO MỚI =====================
        [HttpGet]
            public async Task<IActionResult> Create(string? templateId)
            {
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");

                // Lấy danh sách template thực tế để hiển thị trong Dropdown
                var templates = GetAvailableTemplates();
                // Nếu templateId được truyền từ Gallery, nó sẽ được chọn mặc định trong SelectList
                ViewBag.LandingPages = new SelectList(templates, "Id", "Name", templateId);

                var model = new Event
                {
                    LandingPage = templateId, // Gán mặc định template nếu có tham số truyền vào
                    StartDate = DateTime.Now.AddDays(7),
                    EndDate = DateTime.Now.AddDays(7).AddHours(4)
                };

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(Event @event, IFormFile? imageFile)
            {
                var userId = _userManager.GetUserId(User);

                ModelState.Remove("OrganizerId");
                ModelState.Remove("Category");
                ModelState.Remove("Organizer");
                ModelState.Remove("TemplateConfig");

                if (ModelState.IsValid)
                {
                    try
                    {
                        if (imageFile != null)
                        {
                            @event.ImageUrl = await SaveImage(imageFile);
                        }

                        // XỬ LÝ TEMPLATE CONFIG: Thu thập 6 Sections từ Form
                        var templateMetaFields = Request.Form.Keys
                            .Where(k => k.StartsWith("TemplateMeta."))
                            .ToDictionary(k => k.Replace("TemplateMeta.", ""), k => Request.Form[k].ToString());

                        if (templateMetaFields.Any())
                        {
                            @event.TemplateConfig = JsonSerializer.Serialize(templateMetaFields);
                        }

                        @event.OrganizerId = userId!;
                        @event.CreatedAt = DateTime.Now;
                        @event.Status = "Published";
                        @event.IsActive = true;

                        _context.Add(@event);
                        await _context.SaveChangesAsync();

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                    }
                }

                // Reload dữ liệu nếu lỗi
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
                ViewBag.LandingPages = new SelectList(GetAvailableTemplates(), "Id", "Name", @event.LandingPage);
                return View(@event);
            }

            // ===================== EDIT: CHỈNH SỬA =====================
            [HttpGet]
            public async Task<IActionResult> Edit(int id)
            {
                var userId = _userManager.GetUserId(User);
                var @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);

                if (@event == null) return NotFound();

                // Giải mã JSON để hiển thị lại 6 Sections trong View Edit
                var config = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(@event.TemplateConfig))
                {
                    config = JsonSerializer.Deserialize<Dictionary<string, string>>(@event.TemplateConfig) ?? new Dictionary<string, string>();
                }
                ViewBag.TemplateMeta = config;

                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", @event.CategoryId);
                ViewBag.LandingPages = new SelectList(GetAvailableTemplates(), "Id", "Name", @event.LandingPage);

                return View(@event);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, Event @event, IFormFile? imageFile)
            {
                if (id != @event.Id) return NotFound();
                var userId = _userManager.GetUserId(User);

                var existingEvent = await _context.Events.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);

                if (existingEvent == null) return Unauthorized();

                ModelState.Remove("OrganizerId");
                ModelState.Remove("Category");
                ModelState.Remove("Organizer");
                ModelState.Remove("TemplateConfig");

                if (ModelState.IsValid)
                {
                    try
                    {
                        if (imageFile != null)
                            @event.ImageUrl = await SaveImage(imageFile);
                        else
                            @event.ImageUrl = existingEvent.ImageUrl;

                        // Cập nhật lại Template Config
                        var templateMetaFields = Request.Form.Keys
                            .Where(k => k.StartsWith("TemplateMeta."))
                            .ToDictionary(k => k.Replace("TemplateMeta.", ""), k => Request.Form[k].ToString());

                        @event.TemplateConfig = templateMetaFields.Any()
                            ? JsonSerializer.Serialize(templateMetaFields)
                            : existingEvent.TemplateConfig;

                        @event.OrganizerId = userId!;
                        @event.CreatedAt = existingEvent.CreatedAt;

                        _context.Update(@event);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception)
                    {
                        if (!EventExists(@event.Id)) return NotFound();
                        else throw;
                    }
                }
                return View(@event);
            }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ev = await _context.Events.FindAsync(id);
                if (ev == null) return NotFound();

                // Kiểm tra nếu sự kiện đã có người mua vé thì không cho xóa
                var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
                if (hasBookings) return BadRequest("Cannot delete event with existing bookings.");

                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        // ===================== KHO GIAO DIỆN (GALLERY) =====================
        public IActionResult LandingPageGallery()
            {
                var templates = GetAvailableTemplates();
                return View(templates);
            }

        // ===================== HELPER METHODS =====================

        // Hàm tập trung quản lý danh sách Template để dùng chung nhiều nơi
        //private List<LandingPageTemplate> GetAvailableTemplates()
        //{
        //    return new List<LandingPageTemplate>
        //    {
        //        new LandingPageTemplate { Id = "Charitize", Name = "Charitize - Từ thiện", ImageUrl = "/Templates/Charitize/img/carousel-1.jpg" },
        //        new LandingPageTemplate { Id = "Chefer", Name = "Chefer - Ẩm thực", ImageUrl = "/Templates/Chefer/img/hero-1.jpg" },
        //        new LandingPageTemplate { Id = "KnightOne", Name = "KnightOne - Doanh nghiệp", ImageUrl = "/Templates/KnightOne/assets/img/hero-bg.jpg" },
        //        new LandingPageTemplate { Id = "Medilab", Name = "Medilab - Y tế", ImageUrl = "/Templates/Medilab/assets/img/hero-bg.jpg" },
        //        new LandingPageTemplate { Id = "Medinova", Name = "Medinova - Sức khỏe", ImageUrl = "/Templates/medinova/img/hero.jpg" },
        //        new LandingPageTemplate { Id = "Nova", Name = "Nova - Sáng tạo", ImageUrl = "/Templates/Nova/assets/img/hero/hero-5/hero-img.svg" },
        //        new LandingPageTemplate { Id = "Yummy", Name = "Yummy - Sự kiện Tiệc", ImageUrl = "/Templates/Yummy/assets/img/hero-img.png" }
        //    };
        //}

        private List<LandingPageTemplateEntity> GetAvailableTemplates()
        {
            // Chỉ lấy các Template có trạng thái IsActive = true từ Database
            return _context.LandingPageTemplates
                .Where(t => t.IsActive) // Lọc bỏ các mẫu đã bị Admin khóa
                .Select(t => new LandingPageTemplateEntity
                {
                    Id = t.Id,
                    Name = t.Name,
                    ImageUrl = t.PreviewImageUrl ?? "/images/default-template.png"
                })
                .ToList();
        }

        private async Task<string> SaveImage(IFormFile file)
            {
                string folder = "wwwroot/img/events/";
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), folder, fileName);

                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), folder)))
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), folder));

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return "/img/events/" + fileName;
            }

            private bool EventExists(int id) => _context.Events.Any(e => e.Id == id);

            public class LandingPageTemplateEntity
        {
                public string Id { get; set; } = "";
                public string Name { get; set; } = "";
                public string ImageUrl { get; set; } = "";
                public string Description { get; set; } = "";
            }
        }
    }