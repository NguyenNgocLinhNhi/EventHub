using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using EventManagementSystem.Web.Models.Entities;

namespace EventManagementSystem.Web.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public virtual ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Region { get; set; }   // Tỉnh
        public string? District { get; set; } // Huyện
        public string? Ward { get; set; }     // Xã
        public string? Address { get; set; }  // Số nhà/Đường

        public string OrganizationName { get; set; } = string.Empty;
        public string? OrganizationBio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool NotifySales { get; set; } = false;
        public bool NotifyInquiries { get; set; } = false;
        public bool NotifyEventStatus { get; set; } = true; // Mặc định bật
        public bool NotifyRefunds { get; set; } = true;
        public bool NotifyPayouts { get; set; } = true;
    }
}