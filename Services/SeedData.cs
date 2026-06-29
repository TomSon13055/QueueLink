using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;

namespace QueueLink.Services;

public static class SeedData
{
    // Hardcoded: must match the latest migration class name.
    // Update this if you add/rename migrations.
    private const string CurrentMigrationName = "InitialCreate";

    public static async Task InitializeAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Attempt to apply pending migrations.
        // If the database already has tables from a previous deployment
        // (e.g. migration name changed between deploys), MigrateAsync()
        // throws "relation already exists".  In that case we record the
        // current migration in __EFMigrationsHistory so EF Core treats it
        // as already applied and the app starts normally.
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex) when (IsRelationAlreadyExists(ex))
        {
            await EnsureMigrationRecordedAsync(db);
        }

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
            Slug = "dookki-vincom",
            OwnerId = admin?.Id,
            OpenTime = new TimeOnly(11, 0),
            CloseTime = new TimeOnly(22, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var v2 = new Venue
        {
            Name = "QueueLink Photobooth",
            Description = "Photobooth kỷ niệm tại Shopping Mall",
            Address = "Shopping Mall Floor 2, Quận 7, TP.HCM",
            Phone = "0900000002",
            Slug = "photobooth-mall",
            OwnerId = admin?.Id,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(21, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var v3 = new Venue
        {
            Name = "Safari Food Court",
            Description = "Food court đa dạng tại Safari Park",
            Address = "Safari Park, Quận 9, TP.HCM",
            Phone = "0900000003",
            Slug = "safari-foodcourt",
            OwnerId = admin?.Id,
            OpenTime = new TimeOnly(10, 0),
            CloseTime = new TimeOnly(20, 0),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Venues.AddRange(v1, v2, v3);
        await db.SaveChangesAsync();

        // ── Tables ──────────────────────────────────────────────────────
        var tables = new[]
        {
            new Table { VenueId = v1.Id, Name = "Bàn 1", Capacity = 4, SortOrder = 1, Status = TableStatus.Available },
            new Table { VenueId = v1.Id, Name = "Bàn 2", Capacity = 4, SortOrder = 2, Status = TableStatus.Available },
            new Table { VenueId = v1.Id, Name = "Bàn 3", Capacity = 6, SortOrder = 3, Status = TableStatus.Available },
            new Table { VenueId = v1.Id, Name = "Bàn 4", Capacity = 4, SortOrder = 4, Status = TableStatus.Available },
            new Table { VenueId = v1.Id, Name = "Bàn 5", Capacity = 6, SortOrder = 5, Status = TableStatus.Available },
            new Table { VenueId = v1.Id, Name = "Bàn 6", Capacity = 8, SortOrder = 6, Status = TableStatus.Available },
            new Table { VenueId = v2.Id, Name = "Box 1", Capacity = 4, SortOrder = 1, Status = TableStatus.Available },
            new Table { VenueId = v2.Id, Name = "Box 2", Capacity = 4, SortOrder = 2, Status = TableStatus.Available },
            new Table { VenueId = v3.Id, Name = "Quầy 1", Capacity = 2, SortOrder = 1, Status = TableStatus.Available },
            new Table { VenueId = v3.Id, Name = "Quầy 2", Capacity = 2, SortOrder = 2, Status = TableStatus.Available },
        };
        db.Tables.AddRange(tables);

        // ── Menu Categories & Items (Dookki) ─────────────────────────────
        var catBuffet = new MenuCategory { VenueId = v1.Id, Name = "Buffet", SortOrder = 1 };
        var catKorean = new MenuCategory { VenueId = v1.Id, Name = "Món Hàn", SortOrder = 2 };
        var catDrink = new MenuCategory { VenueId = v1.Id, Name = "Đồ uống", SortOrder = 3 };
        var catDessert = new MenuCategory { VenueId = v1.Id, Name = "Tráng miệng", SortOrder = 4 };
        db.MenuCategories.AddRange(catBuffet, catKorean, catDrink, catDessert);
        await db.SaveChangesAsync();

        db.MenuItems.AddRange(
            new MenuItem { CategoryId = catBuffet.Id, Name = "Buffet Trưa (11:00-14:00)", Description = "Buffet 89k/người", Price = 89000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catBuffet.Id, Name = "Buffet Tối (17:00-22:00)", Description = "Buffet 109k/người", Price = 109000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catKorean.Id, Name = "Tokbokki", Description = "Bánh gối Hàn Quốc", Price = 45000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catKorean.Id, Name = "Kimbap", Description = "Cơm cuộn rong biển", Price = 35000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catKorean.Id, Name = "Gà chiên Hàn", Description = "Gà giòn Cay", Price = 55000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catDrink.Id, Name = "Trà sữa Hàn", Description = "Hương vị Hàn Quốc", Price = 25000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catDrink.Id, Name = "Nước ngọt", Description = "Coca / Sprite", Price = 15000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catDessert.Id, Name = "Bánh gạo hấp", Description = "Tteokbokki ngọt", Price = 30000, IsActive = true, IsAvailable = true },
            new MenuItem { CategoryId = catDessert.Id, Name = "Kem dừa", Description = "Kem que dừa non", Price = 20000, IsActive = true, IsAvailable = true }
        );

        // ── VenueStaff (assign staff to venues) ─────────────────────────
        if (staff != null)
        {
            db.VenueStaff.AddRange(
                new VenueStaff { VenueId = v1.Id, UserId = staff.Id },
                new VenueStaff { VenueId = v2.Id, UserId = staff.Id }
            );
        }

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

    private static bool IsRelationAlreadyExists(Exception ex)
    {
        var pg = ex as Npgsql.PostgresException
            ?? ex.InnerException as Npgsql.PostgresException;
        return pg?.SqlState == "42P07";
    }

    /// <summary>
    /// Creates __EFMigrationsHistory (if missing) and inserts the current
    /// migration name so EF Core treats it as already applied.
    /// </summary>
    private static async Task EnsureMigrationRecordedAsync(ApplicationDbContext db)
    {
        const string historyTable = "__EFMigrationsHistory";

        await db.Database.ExecuteSqlRawAsync($@"
            CREATE TABLE IF NOT EXISTS ""{historyTable}"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK_{historyTable}"" PRIMARY KEY (""MigrationId"")
            )");

        await db.Database.ExecuteSqlRawAsync($@"
            INSERT INTO ""{historyTable}"" (""MigrationId"", ""ProductVersion"")
            VALUES ('{CurrentMigrationName}', '9.0.0')
            ON CONFLICT (""MigrationId"") DO NOTHING");
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
