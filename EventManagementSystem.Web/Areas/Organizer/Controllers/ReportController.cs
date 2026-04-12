using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate, string status)
        {
            var userId = _userManager.GetUserId(User);
            var now = DateTime.Now;

            // 1. Khởi tạo truy vấn sự kiện của Organizer - Include TicketTypes để tính Hiệu suất
            var eventQuery = _context.Events
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings!).ThenInclude(b => b.BookingDetails)
                .Where(e => e.OrganizerId == userId);

            // 2. Lọc theo trạng thái sự kiện
            if (!string.IsNullOrEmpty(status))
            {
                switch (status)
                {
                    case "Upcoming":
                        eventQuery = eventQuery.Where(e => e.StartDate > now);
                        break;
                    case "Ongoing":
                        eventQuery = eventQuery.Where(e => e.StartDate <= now && e.EndDate >= now);
                        break;
                    case "Past":
                        eventQuery = eventQuery.Where(e => e.EndDate < now);
                        break;
                }
            }

            var events = await eventQuery.ToListAsync();

            // 3. Lọc Bookings theo khoảng thời gian thực tế
            var bookingQuery = events.SelectMany(e => e.Bookings ?? new List<Booking>()).AsQueryable();

            if (fromDate.HasValue)
                bookingQuery = bookingQuery.Where(b => b.BookingDate >= fromDate.Value);

            if (toDate.HasValue)
                bookingQuery = bookingQuery.Where(b => b.BookingDate <= toDate.Value.AddDays(1).AddTicks(-1));

            var allBookings = bookingQuery.ToList();
            var allConfirmedBookings = allBookings.Where(b => b.Status == "Confirmed").ToList();

            // --- TRUYỀN THAM SỐ LỌC QUA VIEW ---
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SelectedStatus = status;

            // --- THỐNG KÊ TỔNG QUAN ---
            decimal totalRevenue = allConfirmedBookings
                .SelectMany(b => b.BookingDetails)
                .Where(d => !d.IsCancelled)
                .Sum(d => d.UnitPrice * d.Quantity);
            ViewBag.TotalRevenue = totalRevenue;

            // Thống kê tháng này vs tháng trước của hệ thống
            var sysConfirmedDetails = events.SelectMany(e => e.Bookings ?? new List<Booking>())
                .Where(b => b.Status == "Confirmed")
                .SelectMany(b => b.BookingDetails)
                .Where(d => !d.IsCancelled);

            ViewBag.CurrentMonthRevenue = sysConfirmedDetails
                .Where(d => d.Booking.BookingDate.Month == now.Month && d.Booking.BookingDate.Year == now.Year)
                .Sum(d => d.UnitPrice * d.Quantity);

            var lastMonth = now.AddMonths(-1);
            ViewBag.LastMonthRevenue = sysConfirmedDetails
                .Where(d => d.Booking.BookingDate.Month == lastMonth.Month && d.Booking.BookingDate.Year == lastMonth.Year)
                .Sum(d => d.UnitPrice * d.Quantity);

            // --- BIỂU ĐỒ 12 THÁNG (Theo năm được chọn hoặc hiện tại) ---
            int chartYear = toDate?.Year ?? now.Year;
            ViewBag.ChartYear = chartYear;

            var monthlyRev = new List<decimal>();
            var monthlyCust = new List<int>();

            for (int i = 1; i <= 12; i++)
            {
                monthlyRev.Add(allConfirmedBookings
                    .Where(b => b.BookingDate.Month == i && b.BookingDate.Year == chartYear)
                    .SelectMany(b => b.BookingDetails)
                    .Where(d => !d.IsCancelled)
                    .Sum(d => d.UnitPrice * d.Quantity));
                monthlyCust.Add(allBookings.Where(b => b.BookingDate.Month == i && b.BookingDate.Year == chartYear).Select(b => b.CustomerEmail).Distinct().Count());
            }
            ViewBag.MonthlyRevenueChart = monthlyRev;
            ViewBag.MonthlyCustomerChart = monthlyCust;

            // --- PHƯƠNG THỨC THANH TOÁN ---
            ViewBag.PaymentStats = allConfirmedBookings
                .SelectMany(b => b.BookingDetails)
                .Where(d => !d.IsCancelled)
                .GroupBy(d => d.Booking.PaymentMethod ?? "Other")
                .Select(g => new {
                    Method = g.Key,
                    Amount = g.Sum(x => x.UnitPrice * x.Quantity),
                    Percentage = totalRevenue > 0 ? (g.Sum(x => x.UnitPrice * x.Quantity) * 100m / totalRevenue) : 0m
                }).OrderByDescending(x => x.Amount).ToList();

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyBookingDetails(int month, int year)
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Event.OrganizerId == userId &&
                            b.BookingDate.Month == month &&
                            b.BookingDate.Year == year &&
                            b.Status == "Confirmed")
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new {
                    EventTitle = b.Event.Title,
                    Customer = b.CustomerEmail,
                    Date = b.BookingDate.ToString("dd/MM/yyyy HH:mm"),
                    Amount = b.TotalAmount.ToString("N0") + "đ"
                }).ToListAsync();

            return Json(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyNewCustomers(int month, int year)
        {
            var userId = _userManager.GetUserId(User);

            // Lấy danh sách khách hàng duy nhất dựa trên email trong tháng/năm đã chọn
            var customers = await _context.Bookings
                .Include(b => b.Event)
                .Where(b => b.Event.OrganizerId == userId &&
                            b.BookingDate.Month == month &&
                            b.BookingDate.Year == year)
                .GroupBy(b => b.CustomerEmail) // Nhóm theo email để lấy khách hàng duy nhất
                .Select(g => new {
                    Email = g.Key,
                    Name = g.FirstOrDefault().CustomerName ?? "N/A",
                    Phone = g.FirstOrDefault().PhoneNumber ?? "N/A",
                    TotalBookings = g.Count() // Số lần đặt chỗ trong tháng
                })
                .ToListAsync();

            return Json(customers);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckIn(string ticketCode)
        {
            // 1. Tìm vé trong database
            var ticket = await _context.BookingDetails
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => d.TicketCode == ticketCode);

            if (ticket == null)
                return Json(new { success = false, message = "Vé không hợp lệ!" });

            if (ticket.IsCheckedIn)
                return Json(new { success = false, message = "Vé này đã tham gia rồi!" });

            // 2. Cập nhật trạng thái tham gia
            ticket.IsCheckedIn = true;
            ticket.CheckInTime = DateTime.Now; // Lưu vết thời gian khách đến

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Check-in thành công",
                customer = ticket.Booking.CustomerEmail
            });
        }

        public async Task<IActionResult> EventDetailReport(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Lấy thông tin sự kiện kèm theo tất cả dữ liệu liên quan
            var ev = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings!).ThenInclude(b => b.BookingDetails)
                .Include(e => e.Bookings!).ThenInclude(b => b.BookingDetails).ThenInclude(d => d.TicketType)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);

            if (ev == null) return NotFound();

            // Thống kê nhanh
            var confirmedBookings = ev.Bookings?.Where(b => b.Status == "Confirmed").ToList() ?? new List<Booking>();
            ViewBag.TotalRevenue = confirmedBookings
                .SelectMany(b => b.BookingDetails)
                .Where(d => !d.IsCancelled)
                .Sum(d => d.UnitPrice * d.Quantity);

            ViewBag.TicketsSold = confirmedBookings
                .SelectMany(b => b.BookingDetails)
                .Where(d => !d.IsCancelled)
                .Sum(d => d.Quantity);

            ViewBag.TotalCapacity = ev.TicketTypes?.Sum(t => t.Quantity) ?? 0;

            ViewBag.CheckedInCount = confirmedBookings
                .SelectMany(b => b.BookingDetails)
                .Count(d => d.IsCheckedIn && !d.IsCancelled); // Chỉ tính check-in cho vé hợp lệ

            return View(ev);
        }
    }
}