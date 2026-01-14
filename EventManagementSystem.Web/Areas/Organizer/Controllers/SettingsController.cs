using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SettingsController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Hiển thị trang cài đặt hồ sơ
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user);
        }

        // Cập nhật thông tin cơ bản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string gender, DateTime? birthDate, string region, string district, string ward, string address)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Người dùng chưa đăng nhập" });

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Gender = gender;
            user.BirthDate = birthDate;
            user.Region = region;
            user.District = district;
            user.Ward = ward;
            user.Address = address;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return Json(new { success = true, message = "Cập nhật thành công!" });

            return Json(new { success = false, message = "Lỗi khi lưu dữ liệu" });
        }
    }
}