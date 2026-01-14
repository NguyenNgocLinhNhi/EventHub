using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using EventManagementSystem.Web.Models.Entities;

namespace EventManagementSystem.Web.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public virtual ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Region { get; set; }   // Tỉnh
        public string? District { get; set; } // Huyện
        public string? Ward { get; set; }     // Xã
        public string? Address { get; set; }  // Số nhà/Đường
    }
}