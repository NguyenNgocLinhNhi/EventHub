using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class BookingDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }
        public virtual Booking Booking { get; set; } = null!;

        [Required]
        public int TicketTypeId { get; set; }
        public virtual TicketType TicketType { get; set; } = null!;

        // BỔ SUNG: Lưu vị trí ghế cho từng vé
        public string? SeatNumber { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}