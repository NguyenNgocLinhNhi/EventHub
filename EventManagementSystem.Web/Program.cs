using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===================== C?U HÌNH DATABASE =====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ===================== IDENTITY (CUSTOM USER & ROLE) =====================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true; // B?t bu?c xác nh?n email

        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ===================== C?U HÌNH COOKIE & PHÂN QUY?N =====================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Ch?n truy c?p sai quy?n
    options.LogoutPath = "/Account/Logout";
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Cookie ch? có hi?u l?c trong phiên làm vi?c c?a trình duy?t (Browser Session)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // THAY ??I QUAN TR?NG: Không ??t th?i gian h?t h?n c? ??nh cho Cookie
    // ?i?u này khi?n trình duy?t t? xóa Cookie ngay khi c?a s? b? ?óng hoàn toàn
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
// ===================== D?CH V? EMAIL =====================
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();

// ===================== MVC & RAZOR PAGES =====================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHostedService<BookingCleanupService>();

var app = builder.Build();

// ===================== C?U HÌNH PIPELINE =====================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// TH? T? B?T BU?C: Authentication tr??c Authorization
app.UseAuthentication();
app.UseAuthorization();

// ===================== C?U HÌNH ??NH TUY?N (ROUTING) =====================
// 1. Route cho Area Organizer (?u tiên hàng ??u)
app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Route m?c ??nh cho User (Khách hàng)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ===================== KHỞI TẠO DỮ LIỆU (SEED DATA) =====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        // 1. Tạo các Role mặc định nếu chưa có (Thêm Admin vào danh sách)
        string[] roles = { "Admin", "Organizer", "USER" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. TẠO TÀI KHOẢN ADMIN (Mới thêm)
        var adminEmail = "admin@uef.edu.vn";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                OrganizationName = "Admin",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin@123");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // 3. TẠO TÀI KHOẢN ORGANIZER (Giữ nguyên cấu trúc bạn muốn)
        var userEmail = "nhinnl22@uef.edu.vn";
        var organizer = await userManager.FindByEmailAsync(userEmail);

        if (organizer == null)
        {
            organizer = new ApplicationUser
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "UEF", 
                OrganizationName = "UEF",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(organizer, "123456789");
        }

        if (organizer != null)
        {
            if (!await userManager.IsInRoleAsync(organizer, "Organizer"))
            {
                await userManager.AddToRoleAsync(organizer, "Organizer");
            }
            // Khởi tạo dữ liệu mẫu cho Organizer này
            SeedData.Initialize(context, organizer.Id);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Một lỗi đã xảy ra khi khởi tạo dữ liệu.");
    }
}

app.Run();

