using DocumentFormat.OpenXml.Vml.Spreadsheet;
using EventManagementSystem.Web.Areas.Organizer.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;

using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _context;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }

        // 1. Hiển thị trang cài đặt (Mặc định load dữ liệu vào ViewModel)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Truy vấn dữ liệu từ bảng OrganizationInfos (Dữ liệu mới bạn đã lưu)
            var orgData = await _context.OrganizationInfos.FirstOrDefaultAsync();

            // 2. Lấy danh sách thành viên team
            var team = await _context.TeamMembers.ToListAsync();

            // 3. Ánh xạ dữ liệu vào SettingsViewModel
            var model = new SettingsViewModel
            {
                // Thông tin cá nhân vẫn lấy từ bảng User (Identity)
                FullName = user.FullName,
                PersonalPhone = user.PhoneNumber,
                Gender = user.Gender,
                BirthDate = user.BirthDate,

                // CẬP NHẬT: Lấy thông tin tổ chức từ bảng OrganizationInfos (orgData)
                // Dùng toán tử ?. để tránh lỗi nếu database chưa có bản ghi nào
                OrganizationName = orgData?.OrganizationName,
                OrgType = orgData?.OrgType,
                OrgHotline = orgData?.OrgHotline,
                OrgEmail = orgData?.OrgEmail,
                OrgAddress = orgData?.OrgAddress,
                OrganizationBio = orgData?.OrganizationBio,
                AvatarUrl = orgData?.AvatarUrl, // Đây là Logo công ty

                // Cài đặt thông báo
                NotifySales = user.NotifySales,
                NotifyInquiries = user.NotifyInquiries,
                NotifyEventStatus = user.NotifyEventStatus,
                NotifyRefunds = user.NotifyRefunds,
                NotifyPayouts = user.NotifyPayouts,

                TeamMembers = team
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeamMember()
        {
            var newMember = new TeamMember
            {
                FullName = "New Member",
                Position = "Staff",
                IsVisible = true,
                FacebookUrl = "#",
                ZaloUrl = "#",
                GithubUrl = "#"
            };
            _context.TeamMembers.Add(newMember);
            await _context.SaveChangesAsync();

            // Trả về cả đối tượng member để JS có ID vẽ khung
            return Json(new { success = true, member = new { id = newMember.Id } });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleMemberVisibility(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null) return Json(new { success = false });

            member.IsVisible = !member.IsVisible; // Đảo ngược trạng thái hiện tại
            await _context.SaveChangesAsync();

            return Json(new { success = true, isVisible = member.IsVisible });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeamMember(int id)
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null) return Json(new { success = false });

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Member removed!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTeamMember(TeamMember member, IFormFile? imageFile, bool IsVisible)
        {
            var dbMember = await _context.TeamMembers.FindAsync(member.Id);
            if (dbMember == null) return Json(new { success = false, message = "Member not found." });

            // Cập nhật thông tin cơ bản
            dbMember.FullName = member.FullName;
            dbMember.Position = member.Position;
            dbMember.FacebookUrl = member.FacebookUrl;
            dbMember.GithubUrl = member.GithubUrl;
            var isVisibleRaw = Request.Form["IsVisible"].ToString();
            dbMember.IsVisible = isVisibleRaw.Contains("true");

            // Xử lý upload ảnh thành viên
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/team");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                dbMember.ImageUrl = "/uploads/team/" + uniqueFileName;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Updated successfully!" });
        }

        // 2. Tab: Personal Profile (Sử dụng ViewModel)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            user.FullName = model.FullName;
            user.PhoneNumber = model.PersonalPhone;
            user.Gender = model.Gender;
            user.BirthDate = model.BirthDate;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Json(new { success = true, message = "Personal profile updated successfully!" });

            return Json(new { success = false, message = "Failed to update profile." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBranding(SettingsViewModel model, IFormFile? logoFile)
        {
            // Sử dụng OrganizationInfo để tránh trùng tên với Area Organizer
            var org = await _context.OrganizationInfos.FirstOrDefaultAsync();
            if (org == null)
            {
                org = new OrganizationInfo();
                _context.OrganizationInfos.Add(org);
            }

            org.OrganizationName = model.OrganizationName;
            org.OrgType = model.OrgType;
            org.OrgHotline = model.OrgHotline;
            org.OrgEmail = model.OrgEmail;
            org.OrgAddress = model.OrgAddress;
            org.OrganizationBio = model.OrganizationBio;

            if (logoFile != null && logoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/branding");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + logoFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await logoFile.CopyToAsync(fileStream); }
                org.AvatarUrl = "/uploads/branding/" + uniqueFileName;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Organization info updated successfully!" });
        }

        // 4. Tab: Security (Đổi mật khẩu)
        [HttpPost]
        public async Task<IActionResult> ChangePassword(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            if (string.IsNullOrEmpty(model.OldPassword) || string.IsNullOrEmpty(model.NewPassword))
            {
                return Json(new { success = false, message = "Password fields cannot be empty." });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return Json(new { success = true, message = "Password changed successfully!" });
            }

            return Json(new { success = false, message = result.Errors.FirstOrDefault()?.Description ?? "Failed." });
        }

        // 5. Tab: Notifications
        [HttpPost]
        public async Task<IActionResult> UpdateNotifications(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            // Cập nhật tất cả các loại thông báo
            user.NotifySales = model.NotifySales;
            user.NotifyInquiries = model.NotifyInquiries;
            user.NotifyEventStatus = model.NotifyEventStatus;
            user.NotifyRefunds = model.NotifyRefunds;
            user.NotifyPayouts = model.NotifyPayouts;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Notification preferences updated successfully!" });
            }

            return Json(new { success = false, message = "Could not save settings." });
        }
    }
}