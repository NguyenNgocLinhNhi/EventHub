
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

// ===================== KH?I T?O D? LI?U (SEED DATA) =====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        // 1. T?o các Role m?c ??nh n?u ch?a có
        string[] roles = { "Organizer", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Tìm tài kho?n Công ty A ?? gán quy?n qu?n lý
        var organizer = await userManager.FindByEmailAsync("nhinnl22@uef.edu.vn");

        if (organizer != null)
        {
            // ??m b?o Công ty A có Role Organizer ?? vào ???c Dashboard
            if (!await userManager.IsInRoleAsync(organizer, "Organizer"))
            {
                await userManager.AddToRoleAsync(organizer, "Organizer");
            }

            // Truy?n ID th?t c?a t? ch?c vào ?? gán cho các s? ki?n SeedData
            SeedData.Initialize(context, organizer.Id);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "M?t l?i ?ã x?y ra khi Seed d? li?u.");
    }
}

app.Run();

/*
using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// ??ng ký dùng chung cho toàn b? Project
builder.Services.AddTransient<EmailService, EmailService>();
// C?u hình EmailSettings t? appsettings.json
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
// ===================== DATABASE =====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ===================== IDENTITY (CUSTOM USER) =====================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ===================== MVC =====================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// 1. ??ng ký c?u hình EmailSettings t? appsettings.json
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// 2. ??ng ký EmailService (Dependency Injection)
builder.Services.AddTransient<IEmailService, EmailService>();
var app = builder.Build();


// ===================== PIPELINE =====================
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

// ?? TH? T? B?T BU?C
app.UseAuthentication();
app.UseAuthorization();

// ===================== ROUTING =====================
// 1. Phân lu?ng cho Vùng Organizer (Nhà t? ch?c)
app.MapControllerRoute(
    name: "organizer_area",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Phân lu?ng m?c ??nh cho User (Khách hàng)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

*//*app.MapRazorPages();
*//*app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();*//*

// ===================== KH?I T?O D? LI?U (SEED DATA) =====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Tìm ?úng tài kho?n Công ty A trong Database c?a b?n
    var organizer = await userManager.FindByEmailAsync("nhinnl22@uef.edu.vn");

    if (organizer != null)
    {
        // Truy?n ID th?t c?a t? ch?c vào ?? t?o d? li?u
        SeedData.Initialize(context, organizer.Id);
    }
}

app.Run();*/