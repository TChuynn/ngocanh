using KTXManagement.Demo.Data;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== Đăng ký dịch vụ (Dependency Injection) =====

// EF Core In-Memory Database - không cần cài SQL Server, dữ liệu reset mỗi lần chạy lại app
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("KTXManagementDemoDb"));

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// AIService: xử lý chatbot, gợi ý xếp phòng (UC004), tóm tắt phản ánh (UC007)
builder.Services.AddScoped<IAIService, AIService>();

// Xác thực bằng Cookie - UC001: đăng nhập và phân quyền theo VaiTro (QuanLyKTX/SinhVien/KeToan)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Cho phép gửi AntiForgery token qua header (dùng cho request AJAX/JSON của khung Chat AI)
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

var app = builder.Build();

// ===== Seed dữ liệu mẫu khi khởi động =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    SeedData.Initialize(db);
}

// ===== Middleware pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
