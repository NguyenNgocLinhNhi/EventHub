using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EventManagementSystem.Web.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ===================== MY EVENTS (LỊCH SỬ ĐẶT VÉ) =====================
        // Xử lý phân trang và tự động liên kết vé cũ dựa trên Email
        [HttpGet]
        public async Task<IActionResult> MyEvents(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. LOGIC LIÊN KẾT VÉ GỐC: Tìm vé "vãng lai" (UserId == null) có email trùng khớp
            var anonymousBookings = await _context.Bookings
                .Where(b => b.CustomerEmail == user.Email && b.UserId == null)
                .ToListAsync();

            if (anonymousBookings.Any())
            {
                foreach (var booking in anonymousBookings)
                {
                    booking.UserId = user.Id;
                }
                await _context.SaveChangesAsync();
            }

            // 2. KHỞI TẠO TRUY VẤN LẤY VÉ ĐÃ LIÊN KẾT
            var query = _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.BookingDetails)
                .Where(b => b.UserId == user.Id) // Chỉ cần lọc theo UserId vì bước trên đã liên kết xong
                .OrderByDescending(b => b.BookingDate);

            // 3. XỬ LÝ PHÂN TRANG
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(bookings);
        }

        // ===================== INDEX =====================
        public async Task<IActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Mật khẩu đã được thay đổi thành công."
                : message == ManageMessageId.SetPasswordSuccess ? "Mật khẩu đã được thiết lập."
                : message == ManageMessageId.Error ? "Đã xảy ra lỗi trong quá trình xử lý."
                : "";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new IndexViewModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user) ?? "",
                TwoFactor = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins = await _userManager.GetLoginsAsync(user),
                FullName = user.FullName ?? "",
                Email = user.Email ?? ""
            };

            return View(model);
        }

        // ===================== REMOVE LOGIN =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(ManageLogins));

            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);

            if (!result.Succeeded)
                return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.RemoveLoginSuccess });
        }

        // ===================== ADD PHONE =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Generate token (Optional: use this if you send SMS)
            await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.Number);

            return RedirectToAction(nameof(VerifyPhoneNumber), new { phoneNumber = model.Number });
        }

        // ===================== VERIFY PHONE =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber ?? "", model.Code ?? "");

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to verify phone number");
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.AddPhoneSuccess });
        }

        // ===================== REMOVE PHONE =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.SetPhoneNumberAsync(user, null);

            if (!result.Succeeded)
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        // ===================== CHANGE PASSWORD =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword ?? "", model.NewPassword ?? "");
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
        }

        // ===================== SET PASSWORD =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.AddPasswordAsync(user, model.NewPassword ?? "");

            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.SetPasswordSuccess });
        }

        // ===================== MANAGE LOGINS =====================
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            ViewData["StatusMessage"] = message == ManageMessageId.Error ? "An error has occurred." : "";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return View("Error");

            var userLogins = await _userManager.GetLoginsAsync(user);

            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            var otherLogins = schemes
                .Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider))
                .Select(auth => new SelectListItem
                {
                    Text = auth.DisplayName ?? auth.Name,
                    Value = auth.Name
                })
                .ToList();

            var model = new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins,
                ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1
            };

            return View(model);
        }

        // ===================== LINK LOGIN =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
                return RedirectToAction(nameof(ManageLogins), new { message = ManageMessageId.Error });

            var result = await _userManager.AddLoginAsync(user, info);

            return RedirectToAction(nameof(ManageLogins),
                new { message = result.Succeeded ? (ManageMessageId?)null : ManageMessageId.Error });
        }

        // ===================== EDIT PROFILE =====================
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(new IndexViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(IndexViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = model.FullName ?? "";
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
        }

        // ===================== HELPERS =====================
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
    }
}