using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventManagementSystem.Web.Services;

namespace EventManagementSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        // ===================== LOGIN (GET) =====================
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return View();
        }

        // ===================== LOGIN (POST) =====================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Kiểm tra xem email đã được xác nhận chưa
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Bạn cần xác nhận email trước khi đăng nhập.");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        return RedirectToLocal(returnUrl);
                    }
                }
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        // ===================== REGISTER (GET) =====================
        [AllowAnonymous]
        public IActionResult Register() => View();

        // ===================== REGISTER (POST) =====================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 1. Tạo Token xác nhận email
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    // 2. Tạo link kích hoạt
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: Request.Scheme);

                    // 3. Gửi Email (Bất kỳ ai cũng nhận được qua IEmailService)
                    await _emailService.SendEmailAsync(model.Email, "Xác nhận tài khoản của bạn",
                        $"Vui lòng xác nhận tài khoản bằng cách <a href='{callbackUrl}'>bấm vào đây</a>.");

                    // 4. CHUYỂN HƯỚNG ĐẾN TRANG THÔNG BÁO (Quan trọng)
                    return RedirectToAction("RegisterConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // ===================== TRANG THÔNG BÁO SAU ĐĂNG KÝ =====================
        // ===================== CONFIRM EMAIL =====================

        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // ===================== XÁC NHẬN EMAIL =====================
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // 1. Thực hiện xác nhận Token từ Email
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                // 2. TỰ ĐỘNG ĐĂNG NHẬP: Sau khi xác nhận thành công, cho phép đăng nhập luôn
                // isPersistent: true giúp hệ thống nhớ phiên đăng nhập (lần sau không cần đăng nhập lại ngay)
                await _signInManager.SignInAsync(user, isPersistent: true);

                // Chuyển hướng về trang chủ hoặc trang thông báo thành công
                return RedirectToAction("Index", "Home");
            }

            return View("ConfirmEmailFailed");
        }


        // ===================== LOGOUT =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ===================== HELPER =====================
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}