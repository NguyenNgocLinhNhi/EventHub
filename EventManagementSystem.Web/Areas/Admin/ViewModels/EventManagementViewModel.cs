using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    internal class EventManagementViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;  
        public int TicketsSold { get; set; }
        public int TotalTickets { get; set; }
        public decimal Revenue { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}