using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities; // Đảm bảo đúng namespace của thực thể Event
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Controllers
{
    public class EventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Detail(int id)
        {
            // Eager Loading để nạp đầy đủ thông tin liên quan
            var eventEntity = await _context.Events
                .Include(e => e.TicketTypes)
                .Include(e => e.Category)
                .Include(e => e.Speakers)
                .Include(e => e.Schedules)
                .Include(e => e.Sponsors)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            // ✅ SỬA LỖI CS8600: Sử dụng toán tử ?? để gán giá trị mặc định nếu LandingPage null
            // Cách này vừa xóa cảnh báo vừa làm code gọn hơn
            string landingPageName = eventEntity.LandingPage ?? "Medinova";

            // Nếu landingPageName là chuỗi rỗng (""), vẫn dùng mặc định "Medinova"
            if (string.IsNullOrWhiteSpace(landingPageName))
            {
                landingPageName = "Medinova";
            }

            ViewBag.TemplateName = landingPageName;

            string viewName = $"Detail_{landingPageName}";

            return View(viewName, eventEntity);
        }
    }
}