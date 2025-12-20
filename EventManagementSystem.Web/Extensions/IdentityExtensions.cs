using EventManagementSystem.Web.Models.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventManagementSystem.Web.Extensions
{
    public static class IdentityExtensions
    {
        /// <summary>
        /// Lấy FullName của user hiện tại (ASP.NET Core Identity)
        /// </summary>
        public static async Task<string> GetFullNameAsync(
            this ClaimsPrincipal user,
            UserManager<ApplicationUser> userManager)
        {
            // ✅ SỬA LỖI CS8602: Sử dụng ?. để truy cập Identity an toàn
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return string.Empty;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return string.Empty;

            var appUser = await userManager.FindByIdAsync(userId);
            return appUser?.FullName ?? string.Empty;
        }

        /// <summary>
        /// Phiên bản sync (dùng khi không async được – hạn chế dùng)
        /// </summary>
        public static string GetFullName(
            this ClaimsPrincipal user,
            UserManager<ApplicationUser> userManager)
        {
            // ✅ SỬA LỖI CS8602: Sử dụng ?. để truy cập Identity an toàn
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return string.Empty;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return string.Empty;

            // Lưu ý: Sử dụng .Result có thể gây deadlock, nên dùng Task.Run hoặc tốt nhất là chuyển sang Async hoàn toàn
            var appUser = userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
            return appUser?.FullName ?? string.Empty;
        }
    }
}