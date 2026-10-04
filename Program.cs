using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Hubs;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
builder.Logging.AddFilter("QueueLink", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);

// ── Database ──────────────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found. Set DefaultConnection in appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddDataProtection()
    .SetApplicationName("QueueLink")
    .PersistKeysToDbContext<ApplicationDbContext>();

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
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// ── Session ──────────────────────────────────────────────────────────
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IGuestSession, GuestSession>();

// ── SignalR ─────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IQueueTicketService, QueueTicketService>();

// ── MVC ─────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ── Ensure DataProtectionKeys table exists ──────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('DataProtectionKeys') AND type = 'U')
        BEGIN
            CREATE TABLE DataProtectionKeys (
                Id int NOT NULL IDENTITY PRIMARY KEY,
                FriendlyName nvarchar(max) NULL,
                Xml nvarchar(max) NULL
            );
        END
    ");
}

// ── Ensure QueueTickets.UserId exists ──────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.ExecuteSqlRawAsync(@"
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('QueueTickets') AND type = 'U')
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QueueTickets') AND name = 'UserId')
                ALTER TABLE QueueTickets ADD UserId nvarchar(450) NULL;
        END
    ");
}

// ── Ensure floor-plan layout columns exist ─────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Tables') AND type = 'U')
        BEGIN
            -- Tables table doesn't exist, skip layout columns
            SELECT 1;
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'LayoutX')
                ALTER TABLE Tables ADD LayoutX decimal NOT NULL DEFAULT 50;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'LayoutY')
                ALTER TABLE Tables ADD LayoutY decimal NOT NULL DEFAULT 50;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'LayoutW')
                ALTER TABLE Tables ADD LayoutW decimal NOT NULL DEFAULT 12;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'LayoutH')
                ALTER TABLE Tables ADD LayoutH decimal NOT NULL DEFAULT 9;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Tables') AND name = 'Block')
                ALTER TABLE Tables ADD Block nvarchar(50) NULL;
        END
    ");

    await db.Database.ExecuteSqlRawAsync(@"
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('Tables') AND type = 'U')
        BEGIN
            ;WITH src AS (
                SELECT Id, ROW_NUMBER() OVER (ORDER BY SortOrder) - 1 AS rn
                FROM Tables
                WHERE IsActive = 1 AND LayoutX = 50 AND LayoutY = 50
            )
            UPDATE t
            SET t.LayoutX = 12 + (src.rn % 4) * 22,
                t.LayoutY = 18 + (src.rn / 4) * 22,
                t.LayoutW = 18,
                t.LayoutH = 16
            FROM Tables t
            INNER JOIN src ON t.Id = src.Id;
        END
    ");
}

// ── Seed data ────────────────────────────────────────────────────────
// Skipped for SQL Server - database already has data
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//     await SeedData.InitializeAsync(db, userManager, roleManager);
// }

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
    name: "venueDetail",
    pattern: "venue/{slug}",
    defaults: new { controller = "Public", action = "Index" });

app.MapControllerRoute(
    name: "venues",
    pattern: "venues",
    defaults: new { controller = "Public", action = "Browse" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<QueueHub>("/queueHub");

app.Run();

public partial class Program { }
