using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class TemplateManagementViewModel
    {
        public string Id { get; set; } = string.Empty; //
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PreviewImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; } // Số lượng sự kiện đang dùng template này
    }

    public class CreateTemplateViewModel
    {
        [Required(ErrorMessage = "Mã định danh là bắt buộc (Ví dụ: Charitize)")]
        public string Id { get; set; } = string.Empty; //

        [Required(ErrorMessage = "Tên hiển thị không được để trống")]
        public string Name { get; set; } = string.Empty; //

        public string? Description { get; set; } //

        [Display(Name = "Ảnh xem trước")]
        public IFormFile? PreviewImage { get; set; } // Dùng để upload file
    }
}