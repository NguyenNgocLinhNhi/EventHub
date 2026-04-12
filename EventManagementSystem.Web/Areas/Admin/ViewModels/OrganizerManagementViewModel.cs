namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class OrganizerManagementViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Region { get; set; }
        public int TotalEvents { get; set; } // Đếm từ bảng Event
        public bool IsActive { get; set; } // Dựa trên LockoutEnd
        public string? CurrentRole { get; set; } = "Organizer";
    }
}