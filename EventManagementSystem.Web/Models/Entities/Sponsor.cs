using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Models.Entities
{
    public class Sponsor
    {
        public int Id { get; set; }

        // ===== BASIC INFO =====
        [Required]
        [Display(Name = "Tên đơn vị")]
        [StringLength(150)]
        public required string Name { get; set; }

        [Display(Name = "Logo")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Website")]
        [StringLength(255)]
        public string? WebsiteUrl { get; set; }

        [Display(Name = "Hạng tài trợ")]
        [StringLength(50)]
        public string? Rank { get; set; }    // Kim cương, Vàng, Bạc...

        // ===== RELATION: EVENT =====
        [Required]
        public int EventId { get; set; }

        public virtual Event? Event { get; set; }
    }
}
