using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LIST: Xem danh sách danh mục
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                EventCount = c.Events.Count // Đếm số lượng sự kiện
            }).ToListAsync();
        return View(categories);
    }

    // CREATE & EDIT (Sử dụng Modal hoặc Page riêng)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upsert(CategoryViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                // Thêm mới
                var category = new Category { Name = model.Name, Description = model.Description };
                _context.Categories.Add(category);
            }
            else
            {
                // Cập nhật
                var category = await _context.Categories.FindAsync(model.Id);
                if (category == null) return NotFound();
                category.Name = model.Name;
                category.Description = model.Description;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View("Index", await _context.Categories.ToListAsync());
    }

    // DELETE: Xóa danh mục (Chỉ xóa khi không có sự kiện nào thuộc về nó)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.Include(c => c.Events).FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return Json(new { success = false, message = "Không tìm thấy danh mục" });

        if (category.Events.Any())
        {
            return Json(new { success = false, message = "Không thể xóa danh mục đang có sự kiện!" });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
    // Action xem danh sách sự kiện theo danh mục
    public async Task<IActionResult> GetEventsByCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var events = await _context.Events
            .Where(e => e.CategoryId == id)
            .Select(e => new {
                e.Id,
                e.Title,
                e.Location,
                StartDate = e.StartDate.ToString("dd/MM/yyyy"),
                e.Status
            })
            .ToListAsync();

        return Json(new { categoryName = category.Name, events = events });
    }
}