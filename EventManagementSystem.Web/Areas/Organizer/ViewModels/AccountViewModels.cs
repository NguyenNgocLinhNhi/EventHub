using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Organizer.ViewModels
{
    public class OrganizerRegisterViewModel
    {
        [Required]
        public string OrganizationName { get; set; } = string.Empty;
        [Required]
        public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class OrganizerLoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}