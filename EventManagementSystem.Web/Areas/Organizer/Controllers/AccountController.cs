using EventManagementSystem.Web.Areas.Organizer.ViewModels;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Web.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        // ===================== LOGIN (GET) =====================
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập và là Organizer, chuyển thẳng vào Dashboard
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && !string.IsNullOrEmpty(user.OrganizationName))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Organizer" });
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ===================== LOGIN (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(OrganizerLoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // 1. Kiểm tra xác nhận Email
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Bạn cần xác nhận email trước khi đăng nhập.");
                        return View(model);
                    }

                    // 2. CHẶN TRUY CẬP: Chỉ cho phép tài khoản có OrganizationName đăng nhập tại đây
                    if (string.IsNullOrEmpty(user.OrganizationName))
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập vùng quản trị tổ chức.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        // Luôn đẩy vào Dashboard của Organizer sau khi đăng nhập thành công
                        return RedirectToAction("Index", "Dashboard", new { area = "Organizer" });
                    }
                }
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        // ===================== REGISTER (GET) =====================
        [HttpGet]
        public IActionResult Register() => View();

        // ===================== REGISTER (POST) =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(OrganizerRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    OrganizationName = model.OrganizationName, // Lưu tên công ty thực tế
                    EmailConfirmed = false
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Đảm bảo Role Organizer tồn tại và gán cho User
                    if (!await _roleManager.RoleExistsAsync("Organizer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Organizer"));
                    }
                    await _userManager.AddToRoleAsync(user, "Organizer");

                    // Gửi Email xác nhận với giao diện chuyên nghiệp cho đối tác
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { area = "Organizer", userId = user.Id, token = token }, Request.Scheme);

                    string emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 24px; overflow: hidden;'>
                            <div style='background-color: #00877a; padding: 30px; text-align: center; color: white;'>
                                <h1 style='margin: 0;'>EventHub Organizer</h1>
                            </div>
                            <div style='padding: 40px; text-align: center;'>
                                <h2 style='color: #333;'>Chào mừng đối tác {model.OrganizationName}!</h2>
                                <p style='color: #666; font-size: 16px;'>Vui lòng xác thực email để bắt đầu quản lý sự kiện và theo dõi doanh thu.</p>
                                <a href='{callbackUrl}' style='display: inline-block; padding: 15px 30px; background-color: #00877a; color: white; text-decoration: none; border-radius: 12px; font-weight: bold; margin-top: 20px;'>Xác nhận tài khoản công ty</a>
                            </div>
                        </div>";

                    await _emailService.SendEmailAsync(model.Email, "Xác nhận đối tác Organizer - EventHub", emailBody);

                    return View("RegisterConfirmation");
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // ===================== XÁC NHẬN EMAIL =====================
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View("ConfirmEmailSuccess");
            }
            return View("Error");
        }

        // ===================== LOGOUT =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account", new { area = "Organizer" });
        }
    }
}