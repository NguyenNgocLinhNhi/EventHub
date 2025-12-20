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
        public string Status { get; set; } = null!;

        // ====== RELATION: USER ======
        [Required]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        // ====== RELATION: BOOKING DETAILS ======
        public ICollection<BookingDetail> BookingDetails { get; set; }
            = new List<BookingDetail>();
    }
}
