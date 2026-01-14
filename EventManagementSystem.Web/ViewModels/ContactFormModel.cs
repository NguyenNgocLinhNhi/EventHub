using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.ViewModels
{
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        // Bắt đầu bằng chữ cái, tối thiểu 2 ký tự
        [RegularExpression(@"^[a-zA-Z].{1,}$", ErrorMessage = "Name must start with a letter and have at least 2 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your message.")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters.")]
        public string Message { get; set; }
    }
}
