using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class BookingDetail
    {
        [Key]
        public int Id { get; set; }

        // ====== RELATION: BOOKING ======
        [Required]
        public int BookingId { get; set; }

        public Booking Booking { get; set; } = null!;     // FIX

        // ====== RELATION: TICKET TYPE ======
        [Required]
        public int TicketTypeId { get; set; }

        public TicketType TicketType { get; set; } = null!; // FIX

        // ====== BUSINESS DATA ======
        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
