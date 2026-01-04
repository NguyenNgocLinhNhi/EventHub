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
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ApplicationDbContext context)
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
            // Bảo vệ CS8602: Kiểm tra null cho Identity
            if (User.Identity?.IsAuthenticated == true)
            {
                return await RedirectByUserRole();
            }

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
                    // 1. Kiểm tra xác nhận Email
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty, "Bạn cần xác nhận email trước khi đăng nhập.");
                        return View(model);
                    }

                    // 2. CHẶN TỔ CHỨC: Không cho phép Organization đăng nhập tại cổng User
                    if (!string.IsNullOrEmpty(user.OrganizationName))
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản tổ chức không thể đăng nhập tại giao diện khách hàng.");
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
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                    // OrganizationName mặc định null cho khách hàng cá nhân
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, token = token }, protocol: Request.Scheme);

                    string subject = "Kích hoạt tài khoản EventHub";
                    string body = $@"
                        <div style='font-family: Arial; padding: 20px; border: 1px solid #eee; border-radius: 20px;'>
                            <h2 style='color: #007bff;'>Chào mừng đến với EventHub!</h2>
                            <p>Vui lòng xác nhận email để bắt đầu đặt vé sự kiện:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{callbackUrl}' style='background-color: #28a745; color: white; padding: 12px 25px; text-decoration: none; border-radius: 10px; font-weight: bold;'>XÁC NHẬN NGAY</a>
                            </div>
                        </div>";

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

        // ===================== XÁC NHẬN EMAIL =====================
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return BadRequest();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // Đồng bộ hóa các đơn hàng "mồ côi" của User này
                var orphanBookings = await _context.Bookings
                    .Where(b => b.CustomerEmail == user.Email && b.UserId == null)
                    .ToListAsync();

                foreach (var b in orphanBookings) { b.UserId = user.Id; }
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: true);
                return await RedirectByUserRole();
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

        // ===================== ACCESS DENIED =====================
        [AllowAnonymous]
        public IActionResult AccessDenied(string? message)
        {
            ViewBag.ErrorMessage = message ?? "Tài khoản của bạn không có quyền truy cập vùng này.";
            // Trả về view AccessDenied.cshtml bạn đã tạo
            return View();
        }

        // ===================== HÀM HỖ TRỢ ĐIỀU HƯỚNG =====================
        private async Task<IActionResult> RedirectByUserRole()
        {
            var user = await _userManager.GetUserAsync(User);
            // Fix CS8602: Kiểm tra user != null trước khi truy cập thuộc tính
            if (user != null && !string.IsNullOrEmpty(user.OrganizationName))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Organizer" });
            }
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                // Nếu là Tổ chức mà lỡ vào trang User
                if (user != null && !string.IsNullOrEmpty(user.OrganizationName))
                {
                    return RedirectToAction("AccessDenied", "Account", new { message = "Tài khoản này không có quyền truy cập vùng này. " });
                }
            }

            var allEvents = await _context.Events.ToListAsync();
            return View(allEvents);
        }
    }
}