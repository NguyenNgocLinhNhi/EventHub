using System;
using System.Collections.Generic;

namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Các chỉ số tổng quát
        public int TotalUsers { get; set; }
        public int TotalOrganizers { get; set; }
        public int TotalActiveEvents { get; set; } // Dựa trên Event.Status == "Published" hoặc "Active"
        public int TotalTicketsSold { get; set; }  // Tổng số lượng từ BookingDetail.Quantity
        public long TotalSystemViews { get; set; }
        public decimal TotalCommission { get; set; } // Phí dịch vụ (giả định 10%)

        // Danh sách hiển thị
        public List<RecentEventViewModel> RecentEvents { get; set; } = new();
        public List<RecentTransactionViewModel> RecentTransactions { get; set; } = new();
    }

    public class RecentEventViewModel
    {
        public string Title { get; set; } = string.Empty; // Event.Title
        public string OrganizerName { get; set; } = string.Empty; // Event.Organizer.FullName
        public bool IsApproved { get; set; } // Dựa trên Event.IsActive
        public DateTime CreatedAt { get; set; } // Event.CreatedAt
    }

    public class RecentTransactionViewModel
    {
        public string UserEmail { get; set; } = string.Empty; // Booking.CustomerEmail
        public decimal Amount { get; set; } // Booking.TotalAmount
        public string EventTitle { get; set; } = string.Empty; // Booking.Event.Title
    }

    public class TemplateUsageDetailViewModel
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public List<EventSummaryViewModel> LinkedEvents { get; set; } = new();
    }

    public class EventSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
    }
}