using EventManagementSystem.Web.Models.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Event
    {
        public int Id { get; set; }

        // ===== BASIC INFO =====
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;          // FIX

        [Display(Name = "Description")]
        public string? Description { get; set; }            // OPTIONAL

        [Display(Name = "Cover Image")]
        public string? ImageUrl { get; set; }               // OPTIONAL

        [Required]
        public string Location { get; set; } = null!;       // FIX

        // ===== TIME =====
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        // ===== STATUS =====
        public bool IsActive { get; set; } = true;

        // ===== CATEGORY =====
        [Required]
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;     // FIX

        // ===== TICKETS =====
        public ICollection<TicketType> TicketTypes { get; set; }
            = new List<TicketType>();

        // ===== UI / LANDING =====
        [Display(Name = "Landing Page")]
        public string? LandingPage { get; set; }            // OPTIONAL

        // ===== RELATED ENTITIES =====
        public ICollection<Speaker> Speakers { get; set; }
            = new List<Speaker>();

        public ICollection<Schedule> Schedules { get; set; }
            = new List<Schedule>();

        public ICollection<Sponsor> Sponsors { get; set; }
            = new List<Sponsor>();

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // BỔ SUNG: Liên kết với Organizer (Người tạo sự kiện)
        [Required]
        public string OrganizerId { get; set; } = null!;

        [ForeignKey("OrganizerId")]
        public virtual ApplicationUser Organizer { get; set; } = null!;

    
}
}
