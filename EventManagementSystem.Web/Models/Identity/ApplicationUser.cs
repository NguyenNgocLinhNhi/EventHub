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
    }
}