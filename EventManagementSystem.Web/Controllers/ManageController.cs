using EventManagementSystem.Web.Models.Entities;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventManagementSystem.Web.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ===================== INDEX =====================
        public async Task<IActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Two-factor authentication enabled."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
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

            // ✅ SỬA LỖI CS8601: Sử dụng toán tử ?? để đảm bảo không gán null
            user.FullName = model.FullName ?? "";

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error }); // Hoặc tạo MessageId mới cho UpdateSuccess
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