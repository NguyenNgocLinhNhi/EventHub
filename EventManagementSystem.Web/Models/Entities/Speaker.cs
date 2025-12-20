using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Speaker
    {
        public int Id { get; set; }

        // ===== BASIC INFO =====
        [Required]
        [Display(Name = "Họ tên")]
        [StringLength(100)]
        public string Name { get; set; } = null!;      // REQUIRED

        [Display(Name = "Chức vụ / Nghề nghiệp")]
        [StringLength(150)]
        public string? JobTitle { get; set; }          // OPTIONAL

        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }          // OPTIONAL

        [Display(Name = "Link Facebook / LinkedIn")]
        [StringLength(255)]
        public string? SocialUrl { get; set; }         // OPTIONAL ✅ FIX

        // ===== RELATION: EVENT =====
        [Required]
        public int EventId { get; set; }

        public Event Event { get; set; } = null!;      // REQUIRED
    }
}
