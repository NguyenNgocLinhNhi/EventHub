using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
public class SupportManagerController : Controller
{
    private readonly ApplicationDbContext _context;

    public SupportManagerController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Hiển thị danh sách thắc mắc gửi cho Admin
    public async Task<IActionResult> Index()
    {
        // LỌC: Admin chỉ nhận những câu hỏi có nhãn là Organizer
        var inquiries = await _context.ContactInquiries
            .Where(i => i.Category == "Organizer")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return View(inquiries);
    }

    // Khi Admin bấm vào xem chi tiết/trả lời, hãy cập nhật trạng thái đã xem
    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var inquiry = await _context.ContactInquiries.FindAsync(id);
        if (inquiry == null) return Json(new { success = false });

        inquiry.IsReadByAdmin = true;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // Xử lý trả lời
    [HttpPost]
    public async Task<IActionResult> Reply(int id, string message)
    {
        var inquiry = await _context.ContactInquiries.FindAsync(id);
        if (inquiry == null) return NotFound();

        inquiry.ReplyMessage = message;
        inquiry.IsReplied = true;
        inquiry.RepliedAt = DateTime.Now;
        inquiry.IsReadByAttendee = false; // Để hiện chuông thông báo cho Organizer

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}