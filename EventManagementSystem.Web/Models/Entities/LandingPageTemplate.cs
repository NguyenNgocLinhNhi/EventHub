using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Models.Entities
{
    public class LandingPageTemplate
    {
        [Key]
        public string Id { get; set; } // VD: "Charitize", "Medinova" (Khớp với tên thư mục)

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // VD: "Charitize (Từ thiện)"

        public string? Description { get; set; }

        public string? PreviewImageUrl { get; set; } // Ảnh đại diện hiển thị trong kho

        public bool IsActive { get; set; } = true;
    }
}