using EventManagementSystem.Web.Models.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // BỔ SUNG: Khóa ngoại liên kết tới Event
        [Required]
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public virtual Event Event { get; set; } = null!;

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public string CustomerName { get; set; } = null!;
        [Required]
        public string CustomerEmail { get; set; } = null!;
        public string? PhoneNumber { get; set; }

        public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
    }
}