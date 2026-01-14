using EventManagementSystem.Web.Data;
using Microsoft.EntityFrameworkCore;

public class BookingCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    // Thời gian chờ để demo (tôi để 1 phút cho nhanh khi bạn test)
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(1);

    public BookingCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // GIẢI PHÁP: Tính toán mốc thời gian hết hạn ở phía C#
                // Những đơn hàng nào có BookingDate nhỏ hơn mốc này thì coi như hết hạn
                var expiryTime = DateTime.Now.Subtract(_timeout);

                // Bây giờ EF Core có thể dễ dàng dịch phép so sánh < 
                var expiredBookings = await context.Bookings
                    .Include(b => b.BookingDetails)
                    .Where(b => b.Status == "Pending" && b.BookingDate < expiryTime)
                    .ToListAsync();

                if (expiredBookings.Any())
                {
                    foreach (var booking in expiredBookings)
                    {
                        // Hoàn trả số lượng vé vào kho
                        foreach (var detail in booking.BookingDetails)
                        {
                            var ticketType = await context.TicketTypes.FindAsync(detail.TicketTypeId);
                            if (ticketType != null)
                            {
                                ticketType.Quantity += 1;
                                context.TicketTypes.Update(ticketType);
                            }
                        }
                        // Xóa đơn hàng chưa thanh toán
                        context.Bookings.Remove(booking);
                    }
                    await context.SaveChangesAsync();
                }
            }
            // Kiểm tra mỗi 30 giây một lần
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}