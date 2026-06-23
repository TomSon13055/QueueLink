using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Hubs;
using QueueLink.Integrations.Email;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connString));

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
builder.Services.AddScoped<IEmailSender, GmailSender>();

// ── SignalR ─────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IQueueTicketService, QueueTicketService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IQueueNotificationService, QueueNotificationService>();

// ── MVC ─────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
