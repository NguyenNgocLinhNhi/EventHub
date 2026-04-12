using EventManagementSystem.Web.Areas.Admin.ViewModels;
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.AdminSystemSettings
                                     .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

            var model = new AdminSystemSettingsViewModel
            {
                SystemName = settings.GetValueOrDefault("SystemName", "Eventus"),
                SystemDescription = settings.GetValueOrDefault("SystemDescription", ""),
                SystemLogoUrl = settings.GetValueOrDefault("SystemLogoUrl", "/img/logo.png"),
                DefaultTimeZone = settings.GetValueOrDefault("DefaultTimeZone", "SE Asia Standard Time"),
                SmtpServer = settings.GetValueOrDefault("SmtpServer", ""),
                SmtpPort = int.Parse(settings.GetValueOrDefault("SmtpPort", "587")),
                SmtpUser = settings.GetValueOrDefault("SmtpUser", ""),
                SmtpPass = settings.GetValueOrDefault("SmtpPass", ""),

                EnableSsl = bool.Parse(settings.GetValueOrDefault("EnableSsl", "false"))
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(AdminSystemSettingsViewModel model, IFormFile? logoFile)
        {
            try
            {
                // 1. Xử lý upload Logo (Giữ nguyên)
                if (logoFile != null && logoFile.Length > 0)
                {
                    string fileName = "system_logo" + Path.GetExtension(logoFile.FileName);
                    string path = Path.Combine(_webHostEnvironment.WebRootPath, "img", fileName);
                    using (var stream = new FileStream(path, FileMode.Create)) { await logoFile.CopyToAsync(stream); }
                    await PrepareSetting("SystemLogoUrl", "/img/" + fileName);
                }

                // 2. Chuyển TẤT CẢ sang dùng PrepareSetting để an toàn với giá trị NULL
                await PrepareSetting("SystemName", model.SystemName);
                await PrepareSetting("SystemDescription", model.SystemDescription);
                await PrepareSetting("DefaultTimeZone", model.DefaultTimeZone);
                await PrepareSetting("SmtpServer", model.SmtpServer);
                await PrepareSetting("SmtpPort", model.SmtpPort.ToString());
                await PrepareSetting("SmtpUser", model.SmtpUser);
                if (!string.IsNullOrEmpty(model.SmtpPass))
                {
                    await PrepareSetting("SmtpPass", model.SmtpPass);
                }
                await PrepareSetting("EnableSsl", model.EnableSsl.ToString());

                // 3. Chỉ gọi SaveChanges MỘT LẦN duy nhất tại đây
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Saved successfully!" });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Database Error: " + errorMsg });
            }
        }

        // Hàm bổ trợ: Chỉ chuẩn bị dữ liệu (không Save ngay)
        private async Task PrepareSetting(string key, string value)
        {            
            string safeValue = value ?? string.Empty;

            var setting = await _context.AdminSystemSettings
                                         .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                _context.AdminSystemSettings.Add(new AdminSystemSetting
                {
                    SettingKey = key,
                    SettingValue = safeValue // Không bao giờ là NULL
                });
            }
            else
            {
                setting.SettingValue = safeValue; // Cập nhật giá trị an toàn
            }
        }

        private async Task SaveSetting(string key, string value)
        {
            var setting = await _context.AdminSystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null) _context.AdminSystemSettings.Add(new AdminSystemSetting { SettingKey = key, SettingValue = value });
            else setting.SettingValue = value;
            await _context.SaveChangesAsync();
        }
    }
}
