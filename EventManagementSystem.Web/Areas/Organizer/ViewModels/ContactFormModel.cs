using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Organizer.ViewModels
{
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        // Ràng buộc: Bắt đầu bằng chữ cái, tối thiểu 2 ký tự
        [RegularExpression(@"^[a-zA-Z].{1,}$", ErrorMessage = "Name must start with a letter and have at least 2 characters.")]
        public string FullName { get; set; } 

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Message content is required.")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters long.")]
        public string Message { get; set; }
    }
}