namespace EventManagementSystem.Web.Areas.Organizer.ViewModels
{
    public class OrganizerDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTickets { get; set; }
        public int TotalEvents { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalViews { get; set; } // Số lượt xem (nếu có tracking)

        public List<EventManagementSystem.Web.Models.Entities.Event> RecentEvents { get; set; } = new();
        public List<EventManagementSystem.Web.Models.Entities.Booking> RecentBookings { get; set; } = new();
    }

    public class RecentEventViewModel
    {
        public string Title { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public int SoldQuantity { get; set; }
        public int TotalCapacity { get; set; }
        public decimal Revenue { get; set; }
        // Thuộc tính tính toán phần trăm thanh Progress Bar
        public double ProgressPercentage => TotalCapacity > 0 ? (double)SoldQuantity / TotalCapacity * 100 : 0;
    }
}