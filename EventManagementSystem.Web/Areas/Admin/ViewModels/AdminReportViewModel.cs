namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class AdminReportViewModel
    {
        // Thống kê tổng quan
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalEvents { get; set; }
        public decimal AvgRevenuePerEvent => TotalEvents > 0 ? TotalRevenue / TotalEvents : 0;

        // Dữ liệu biểu đồ xu hướng 7 tháng
        public List<MonthlyStat> RevenueTrend { get; set; } = new List<MonthlyStat>(); 

        // Dữ liệu Top Organizers
        public List<OrganizerStat> TopOrganizers { get; set; } = new List<OrganizerStat>();

        // Chi tiết hiệu suất hàng tháng
        public List<MonthlyStat> MonthlyPerformance { get; set; } = new List<MonthlyStat>();
    }

    public class MonthlyStat
    {
        public string Month { get; set; } = string.Empty;
        public int EventsCount { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Growth { get; set; }
    }

    public class OrganizerStat
    {
        public string Name { get; set; } = string.Empty;
        public int EventsCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
