using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventManagementSystem.Web.ViewModels
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }

        public IList<UserLoginInfo> Logins { get; set; }
            = new List<UserLoginInfo>();

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }

        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
            = new List<UserLoginInfo>();

        public IList<SelectListItem> OtherLogins { get; set; }
            = new List<SelectListItem>();

        public bool ShowRemoveButton { get; set; }
    }

    public class FactorViewModel
    {
        // ✅ SỬA LỖI CS8618: Gán giá trị mặc định là chuỗi rỗng
        public string Purpose { get; set; } = string.Empty;
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100,
            ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword",
            ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100,
            ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword",
            ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        // ✅ SỬA LỖI CS8618: Gán giá trị mặc định
        public string Number { get; set; } = string.Empty;
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        // ✅ SỬA LỖI CS8618
        public string Code { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        // ✅ SỬA LỖI CS8618
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ConfigureTwoFactorViewModel
    {
        public string? SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }
            = new List<SelectListItem>();
    }
}