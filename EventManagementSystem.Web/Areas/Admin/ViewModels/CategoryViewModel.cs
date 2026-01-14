using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Admin.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public int EventCount { get; set; } // Số lượng sự kiện thuộc danh mục này
    }
}