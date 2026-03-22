using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using EventManagementSystem.Web.Models.Entities;

namespace EventManagementSystem.Web.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        // Thêm biến slug và status
        public string? Slug { get; set; }      // link: /org/abc-event
        public bool IsApproved { get; set; } = false;    // Admin duyệt

        // Liên kết với events
        public virtual ICollection<Event> Events { get; set; }
            = new List<Event>();

        public virtual ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Region { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? Address { get; set; }
    }
}