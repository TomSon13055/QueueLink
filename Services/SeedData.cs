using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;

namespace QueueLink.Services;

public static class SeedData
{
    public static async Task InitializeAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await db.Database.MigrateAsync();

        // ── Roles ─────────────────────────────────────────────────────
        await CreateRoleAsync(roleManager, "Admin");
        await CreateRoleAsync(roleManager, "Staff");
        await CreateRoleAsync(roleManager, "Customer");

        // ── Users ─────────────────────────────────────────────────────
        var admin = await CreateUserAsync(userManager,
            "admin@queuelink.com", "Admin@123", "Admin User");
        var staff = await CreateUserAsync(userManager,
            "staff@queuelink.com", "Staff@123", "Staff Member");

        if (admin != null) await userManager.AddToRoleAsync(admin, "Admin");
        if (staff != null) await userManager.AddToRoleAsync(staff, "Staff");

        // ── Venues ────────────────────────────────────────────────────
        if (await db.Venues.AnyAsync()) return; // already seeded

        var v1 = new Venue
        {
            Name = "Dookki Buffet Vincom",
            Description = "Nhà hàng buffet Hàn Quốc nổi tiếng tại Vincom Center",
            Address = "Vincom Center, Quận 1, TP.HCM",
            Phone = "0900000001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var v2 = new Venue
        {
            Name = "QueueLink Photobooth",
            Description = "Photobooth kỷ niệm tại Shopping Mall",
            Address = "Shopping Mall Floor 2, Quận 7, TP.HCM",
            Phone = "0900000002",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var v3 = new Venue
        {
            Name = "Safari Food Court",
            Description = "Food court đa dạng tại Safari Park",
            Address = "Safari Park, Quận 9, TP.HCM",
            Phone = "0900000003",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Venues.AddRange(v1, v2, v3);
        await db.SaveChangesAsync();

        // ── Queue Services ─────────────────────────────────────────────
        db.QueueServices.AddRange(
            new QueueService
            {
                VenueId = v1.Id,
                Name = "Buffet Table Queue",
                Description = "Hàng chờ lấy bàn buffet",
                Prefix = "A",
                AverageServiceMinutes = 8,
                QueueStatus = QueueStatus.Open,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new QueueService
            {
                VenueId = v2.Id,
                Name = "Photobooth Session Queue",
                Description = "Hàng chờ chụp ảnh photobooth",
                Prefix = "P",
                AverageServiceMinutes = 5,
                QueueStatus = QueueStatus.Open,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new QueueService
            {
                VenueId = v3.Id,
                Name = "Take-away Counter",
                Description = "Quầy lấy đồ ăn mang đi",
                Prefix = "T",
                AverageServiceMinutes = 3,
                QueueStatus = QueueStatus.Paused,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task CreateRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName)) return;
        await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    private static async Task<ApplicationUser?> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email, string password, string fullName)
    {
        if (await userManager.FindByEmailAsync(email) != null) return null;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName
        };
        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded ? user : null;
    }
}
