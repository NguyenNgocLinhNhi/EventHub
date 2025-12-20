using EventManagementSystem.Web.Data;
using EventManagementSystem.Web.Models;
using EventManagementSystem.Web.Models.Identity;
using EventManagementSystem.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
        options.SignIn.RequireConfirmedAccount = false;

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
// Dòng này c?c k? quan tr?ng ?? s?a l?i b?n ?ang g?p
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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// --- THÊM ?O?N NÀY ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        SeedData.Initialize(context); // G?i hàm kh?i t?o
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "M?t l?i ?ã x?y ra khi n?p d? li?u m?u.");
    }
}
app.Run();
