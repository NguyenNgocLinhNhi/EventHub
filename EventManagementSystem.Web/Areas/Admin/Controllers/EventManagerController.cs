using ClosedXML.Excel;
using EventManagementSystem.Web.Areas.Admin.ViewModels;
using EventManagementSystem.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventManagerController(ApplicationDbContext context) => _context = context;

        // Trang quản lý chính: Hiển thị Dashboard và danh sách tổng hợp
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;

            // 1. Tính toán thống kê Dashboard
            ViewBag.Stats = new
            {
                Total = await _context.Events.CountAsync(),
                Upcoming = await _context.Events.CountAsync(e => e.StartDate > now),
                Ongoing = await _context.Events.CountAsync(e => e.StartDate <= now && e.EndDate >= now),
                // Doanh thu thực tế từ đơn hàng đã xác nhận
                Revenue = await _context.Bookings.Where(b => b.Status == "Confirmed").SumAsync(b => b.TotalAmount)
            };

            // 2. Lấy danh sách sự kiện kèm tiến độ vé và doanh thu từng mục
            var eventList = await _context.Events
                .Select(e => new EventManagementViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    OrganizerName = e.Organizer != null ? e.Organizer.OrganizationName : "Hệ thống",
                    StartDate = e.StartDate,
                    Location = e.Location,
                    // Số vé đã bán (đếm từ BookingDetails đã xác nhận)
                    TicketsSold = _context.BookingDetails.Count(d => d.Booking.EventId == e.Id && d.Booking.Status == "Confirmed"),
                    // Tổng kho vé
                    TotalTickets = _context.TicketTypes.Where(t => t.EventId == e.Id).Sum(t => t.Quantity),
                    // Doanh thu riêng từng sự kiện
                    Revenue = _context.Bookings.Where(b => b.EventId == e.Id && b.Status == "Confirmed").Sum(b => b.TotalAmount),
                    Status = e.StartDate > now ? "Upcoming" : (e.EndDate < now ? "Completed" : "Ongoing")
                }).ToListAsync();

            return View(eventList);
        }

        // Action: Phục vụ nút "Event approval" trên Sidebar
        // Hiển thị danh sách các sự kiện đang ở trạng thái chờ duyệt (Pending)
        public async Task<IActionResult> ApprovalList()
        {
            var pendingEvents = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Category)
                .Where(e => e.Status == "Pending" || e.IsActive == false)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            return View(pendingEvents);
        }

        // Xem chi tiết sự kiện
        public async Task<IActionResult> Details(int id)
        {
            // Phải Include Organizer, Category và TicketTypes để View hiển thị được
            var @event = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            return View(@event);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = "Upcoming"; // Cập nhật trạng thái hiển thị
            ev.IsActive = true;     // Kích hoạt sự kiện

            await _context.SaveChangesAsync();
            // có thể thêm logic gửi Email thông báo tại đây
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Status = "Rejected"; // Cập nhật trạng thái từ chối
            ev.IsActive = false;    // Ẩn sự kiện

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            // 1. Lấy dữ liệu thực tế tương tự như trang Index
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Category)
                .Select(e => new {
                    e.Title,
                    Organizer = e.Organizer != null ? e.Organizer.OrganizationName : "System",
                    e.StartDate,
                    e.Location,
                    Category = e.Category != null ? e.Category.Name : "General",
                    // Tính toán vé và doanh thu thực tế
                    Sold = _context.BookingDetails.Count(d => d.Booking.EventId == e.Id && d.Booking.Status == "Confirmed"),
                    Revenue = _context.Bookings.Where(b => b.EventId == e.Id && b.Status == "Confirmed").Sum(b => b.TotalAmount),
                    e.Status
                }).ToListAsync();

            // 2. Tạo file Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Events Report");
                var currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "Event Title";
                worksheet.Cell(currentRow, 2).Value = "Organizer";
                worksheet.Cell(currentRow, 3).Value = "Start Date";
                worksheet.Cell(currentRow, 4).Value = "Location";
                worksheet.Cell(currentRow, 5).Value = "Tickets Sold";
                worksheet.Cell(currentRow, 6).Value = "Revenue";
                worksheet.Cell(currentRow, 7).Value = "Status";

                // Style cho Header
                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Data
                foreach (var item in events)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.Title;
                    worksheet.Cell(currentRow, 2).Value = item.Organizer;
                    worksheet.Cell(currentRow, 3).Value = item.StartDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(currentRow, 4).Value = item.Location;
                    worksheet.Cell(currentRow, 5).Value = item.Sold;
                    worksheet.Cell(currentRow, 6).Value = item.Revenue;
                    worksheet.Cell(currentRow, 7).Value = item.Status;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Events_Report.xlsx");
                }
            }
        }
    }
}
/*namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] //
    [Authorize(Roles = "Admin")]

    public class EventManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventManagerController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Organizer) // Lấy thông tin nhà tổ chức
                .Include(e => e.Category)  // Lấy danh mục sự kiện
                .ToListAsync();
            return View(events);
        }
        public async Task<IActionResult> Details(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Organizer) //
                .Include(e => e.Category) //
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null) return NotFound();

            return View(@event);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            ev.IsActive = true; // Phê duyệt sự kiện
            await _context.SaveChangesAsync();
            return Ok();
        }

       
    }
}*/