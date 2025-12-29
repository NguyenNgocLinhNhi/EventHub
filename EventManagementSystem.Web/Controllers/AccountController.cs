using EventManagementSystem.Web.Data; 
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using EventManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventManagementSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context; // Thêm DbContext để quản lý dữ liệu vé 

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ApplicationDbContext context) // Inject DbContext vào Constructor 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _context = context;
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
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: Request.Scheme);

                    string subject = "Kích hoạt tài khoản Event Management";
                    string body = $@"
                        <div style='font-family: Arial; padding: 20px; border: 1px solid #eee;'>
                            <h2 style='color: #007bff;'>Chào mừng bạn đến với hệ thống sự kiện!</h2>
                            <p>Cảm ơn bạn đã đăng ký. Vui lòng nhấn vào nút bên dưới để xác nhận email và bắt đầu sử dụng tài khoản:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{callbackUrl}' style='background-color: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>XÁC NHẬN TÀI KHOẢN</a>
                            </div>
                            <p style='color: #666; font-size: 12px;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>
                        </div>";

                   // Gửi email kích hoạt duy nhất một lần với giao diện đẹp 
                    await _emailService.SendEmailAsync(model.Email, subject, body);

                    return RedirectToAction("RegisterConfirmation");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // ===================== XÁC NHẬN EMAIL =====================
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return BadRequest();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Đồng bộ hóa: Tìm vé cũ đặt qua Email này nhưng chưa có UserId
                var orphanBookings = await _context.Bookings
                    .Where(b => b.CustomerEmail == user.Email && b.UserId == null)
                    .ToListAsync();

                foreach (var b in orphanBookings)
                {
                    b.UserId = user.Id;
                }
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: true);
                return RedirectToAction("Index", "Home");
            }
            return View("Error");
        }

        // ===================== LOGOUT =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}