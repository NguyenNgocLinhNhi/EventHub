using EventManagementSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Web.Areas.Organizer.ViewModels
{
    public class SettingsViewModel
    {
        // --- Tab 1: Personal Profile ---
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; }

        public string? PersonalPhone { get; set; }

        public string? Gender { get; set; }

        public DateTime? BirthDate { get; set; }

        public string? Position { get; set; } // Chức vụ


        // --- Tab 2: Organization Info (Branding) ---
        public string? OrganizationName { get; set; }
        public string? OrganizationBio { get; set; }
        public string? OrgHotline { get; set; } // Hotline công khai
        public string? OrgEmail { get; set; } // Email liên hệ công việc
        public string? OrgAddress { get; set; } // Địa chỉ trụ sở
        public string? OrgType { get; set; } // Loại hình tổ chức
        public string? AvatarUrl { get; set; }


        // --- Tab 3: Security (Change Password) ---
        [DataType(DataType.Password)]
        public string? OldPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }


        // --- Tab 4: Notifications ---
        public bool NotifySales { get; set; }
        public bool NotifyInquiries { get; set; }
        public bool NotifyEventStatus { get; set; }
        public bool NotifyRefunds { get; set; }
        public bool NotifyPayouts { get; set; }

        public List<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}