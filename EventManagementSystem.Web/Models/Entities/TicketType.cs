using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Web.Models.Entities
{
    public class TicketType
    {
        public int Id { get; set; }

        // ===== BASIC INFO =====
        [Required]
        [StringLength(50)]
        // ✅ Sửa: Gán giá trị mặc định là chuỗi rỗng
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Quantity Available")]
        public int Quantity { get; set; }

        // ===== RELATION: EVENT =====
        [Required]
        public int EventId { get; set; }

        // ✅ Sửa: Dùng toán tử ! (null-forgiving) vì EF Core sẽ tự nạp dữ liệu này
        public virtual Event Event { get; set; } = default!;

        // ===== RELATION: BOOKING DETAILS =====
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
            = new List<BookingDetail>();
    }
}