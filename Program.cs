using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Hubs;
using QueueLink.Integrations.Email;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────
// Railway giới hạn 500 logs/sec — giới hạn console output.
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
builder.Logging.AddFilter("QueueLink", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);

// ── Database ──────────────────────────────────────────────────────────
// Railway cung cấp DATABASE_URL (postgresql://...). Nếu có → dùng PostgreSQL.
// Nếu không → dùng SQL Server LocalDB từ appsettings.json (local dev).
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway uses postgres:// or postgresql://
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var pgUser = userInfo[0];
    var pgPass = userInfo.Length > 1 ? userInfo[1] : "";
    var pgHost = uri.Host;
    var pgPort = uri.Port > 0 ? uri.Port : 5432;
    var pgDb = uri.AbsolutePath.TrimStart('/');

    connString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};SSL Mode=Require;Trust Server Certificate=true";
}

if (string.IsNullOrEmpty(connString))
    throw new InvalidOperationException("Connection string not found. Set DefaultConnection in appsettings.json or DATABASE_URL env var.");

if (!string.IsNullOrEmpty(databaseUrl))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connString);
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });

    // Persist antiforgery / session keys to PostgreSQL so they survive container restarts.
    builder.Services.AddDataProtection()
        .SetApplicationName("QueueLink")
        .PersistKeysToDbContext<ApplicationDbContext>();
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connString);
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
}

// ── Identity ─────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
        // Không bắt buộc xác nhực email để login; controller sẽ enforce riêng
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Session (lưu thông tin guest để auto-fill form lấy số) ────────────
var sessionTimeoutMin = builder.Configuration.GetValue<int?>("Session:TimeoutMinutes") ?? 10080;
var sessionCookieName = builder.Configuration.GetValue<string>("Session:CookieName") ?? "QueueLink.Session";

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = sessionCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMin);
});

// HttpContextAccessor + GuestSession service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IGuestSession, GuestSession>();

// ── Email integration ────────────────────────────────────────────────
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailProvider = builder.Configuration.GetValue<string>("Email:Provider") ?? "Gmail";
if (emailProvider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailSender, ResendSender>();
}
else if (emailProvider.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailSender, BrevoSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, GmailSender>();
}

// ── SignalR ─────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IQueueTicketService, QueueTicketService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IQueueNotificationService, QueueNotificationService>();

// ── MVC ─────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// Railway cung cấp biến PORT — dùng nó; không có thì dùng 8080.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ── Seed data ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedData.InitializeAsync(db, userManager, roleManager);
}

// ── HTTP pipeline ────────────────────────────────────────────────────
// Railway terminate SSL ở load balancer → app nhận HTTP. Tắt redirect để tránh loop.
var isRailway = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    if (!isRailway) app.UseHsts();
}

if (!isRailway) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// ── Routes ──────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── SignalR hub mapping ──────────────────────────────────────────────
app.MapHub<QueueHub>("/queueHub");

app.Run();

// Make Program class partial so generated test base class can inherit.
public partial class Program { }
