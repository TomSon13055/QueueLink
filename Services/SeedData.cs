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
        // Skip MigrateAsync - database already exists with tables
        // await db.Database.MigrateAsync();

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
        if (await db.Venues.AnyAsync())
        {
            // Database already has venues (likely seeded on a previous deploy
            // when LogoUrl / CoverImageUrl were not part of the model).
            // Backfill any venue whose images are still null so the public
            // detail page and queue cards stop showing the icon fallback.
            await BackfillVenueImagesAsync(db);
            return;
        }

        // Demo image set: 1 logo + 1 cover per venue, served from Unsplash
        // CDN (no auth required). Real venues will replace these via
        // /Owner/VenueSettings. Keep the public page usable out-of-the-box.
        var v1 = new Venue
        {
            Name = "Dookki Buffet Vincom",
            Description = "Nhà hàng buffet Hàn Quốc nổi tiếng tại Vincom Center",
            Address = "Vincom Center, Quận 1, TP.HCM",
            Phone = "0900000001",
            Slug = "dookki-vincom",
            LogoUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=200&h=200&fit=crop",
            CoverImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200&h=600&fit=crop",
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
            LogoUrl = "https://images.unsplash.com/photo-1527526029430-319f10814151?w=200&h=200&fit=crop",
            CoverImageUrl = "https://images.unsplash.com/photo-1527526029430-319f10814151?w=1200&h=600&fit=crop",
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
            LogoUrl = "https://images.unsplash.com/photo-1565958011703-44f9829ba187?w=200&h=200&fit=crop",
            CoverImageUrl = "https://images.unsplash.com/photo-1565958011703-44f9829ba187?w=1200&h=600&fit=crop",
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
            // Layout: 3 bàn mỗi hàng, 16% width, 11% height.
            new Table { VenueId = v1.Id, Name = "Bàn 1", Capacity = 4, SortOrder = 1, Status = TableStatus.Available, Block = "Tầng 1", LayoutX = 16, LayoutY = 22, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v1.Id, Name = "Bàn 2", Capacity = 4, SortOrder = 2, Status = TableStatus.Available, Block = "Tầng 1", LayoutX = 50, LayoutY = 22, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v1.Id, Name = "Bàn 3", Capacity = 6, SortOrder = 3, Status = TableStatus.Available, Block = "Tầng 1", LayoutX = 84, LayoutY = 22, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v1.Id, Name = "Bàn 4", Capacity = 4, SortOrder = 4, Status = TableStatus.Available, Block = "Tầng 1", LayoutX = 16, LayoutY = 50, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v1.Id, Name = "Bàn 5", Capacity = 6, SortOrder = 5, Status = TableStatus.Available, Block = "Tầng 1", LayoutX = 50, LayoutY = 50, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v1.Id, Name = "Bàn 6", Capacity = 8, SortOrder = 6, Status = TableStatus.Available, Block = "VIP", LayoutX = 84, LayoutY = 50, LayoutW = 16, LayoutH = 14 },
            new Table { VenueId = v2.Id, Name = "Box 1", Capacity = 4, SortOrder = 1, Status = TableStatus.Available, Block = "Box", LayoutX = 30, LayoutY = 35, LayoutW = 22, LayoutH = 22 },
            new Table { VenueId = v2.Id, Name = "Box 2", Capacity = 4, SortOrder = 2, Status = TableStatus.Available, Block = "Box", LayoutX = 70, LayoutY = 35, LayoutW = 22, LayoutH = 22 },
            new Table { VenueId = v3.Id, Name = "Quầy 1", Capacity = 2, SortOrder = 1, Status = TableStatus.Available, Block = "Quầy", LayoutX = 30, LayoutY = 40, LayoutW = 20, LayoutH = 18 },
            new Table { VenueId = v3.Id, Name = "Quầy 2", Capacity = 2, SortOrder = 2, Status = TableStatus.Available, Block = "Quầy", LayoutX = 70, LayoutY = 40, LayoutW = 20, LayoutH = 18 },
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

    /// <summary>
    /// Backfill venue images for older databases where LogoUrl / CoverImageUrl
    /// were null or contained broken image URLs.
    /// </summary>
    private static async Task BackfillVenueImagesAsync(ApplicationDbContext db)
    {
        // Only fill rows where BOTH LogoUrl and CoverImageUrl are
        // null. If an owner already saved a URL (even one we don't
        // love the look of) leave it alone — they're the source of
        // truth now and overriding their input on every deploy is a
        // worse experience than a leftover Google Imgres URL.
        var rows = await db.Venues
            .Where(v => v.LogoUrl == null || v.CoverImageUrl == null)
            .Select(v => new { v.Id, v.Slug })
            .ToListAsync();

        // Separate query: rows where the saved URL is *clearly*
        // unusable (Google Imgres HTML redirect, not an image).
        // We detect this by looking for the search-results redirect
        // path AND a URL-encoded `imgurl=` parameter inside — that's
        // what Google Image Search serves to its UI, and the
        // <img src> will never resolve to a real image. Real Google
        // storage URLs (e.g. storage.googleapis.com) won't match.
        var brokenRows = await db.Venues
            .Where(v => (v.LogoUrl != null
                          && v.LogoUrl.Contains("/imgres?")
                          && v.LogoUrl.Contains("imgurl="))
                     || (v.CoverImageUrl != null
                          && v.CoverImageUrl.Contains("/imgres?")
                          && v.CoverImageUrl.Contains("imgurl=")))
            .Select(v => new { v.Id, v.Slug })
            .ToListAsync();

        var allRows = rows
            .Concat(brokenRows)
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .ToList();

        if (allRows.Count == 0) return;

        foreach (var row in allRows)
        {
            var (logo, cover) = GetDefaultImagesForSlug(row.Slug);
            await db.Database.ExecuteSqlRawAsync(
                $@"UPDATE Venues
                   SET LogoUrl      = CASE
                                            WHEN LogoUrl IS NULL
                                                 OR (LogoUrl LIKE '%/imgres?%'
                                                     AND LogoUrl LIKE '%imgurl=%')
                                                 THEN '{logo}'
                                            ELSE LogoUrl
                                          END,
                       CoverImageUrl = CASE
                                            WHEN CoverImageUrl IS NULL
                                                 OR (CoverImageUrl LIKE '%/imgres?%'
                                                     AND CoverImageUrl LIKE '%imgurl=%')
                                                 THEN '{cover}'
                                            ELSE CoverImageUrl
                                          END
                   WHERE Id = {row.Id};");
        }
    }

    /// <summary>
    /// Per-venue default images. If a slug isn't recognised (e.g. the
    /// owner created the venue manually), fall back to the Dookki set
    /// so something reasonable still shows.
    /// </summary>
    private static (string logo, string cover) GetDefaultImagesForSlug(string? slug)
    {
        return slug switch
        {
            "dookki-vincom" => (
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=200&h=200&fit=crop",
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200&h=600&fit=crop"),
            "photobooth-mall" => (
                // Verified-working Unsplash IDs (each one returns HTTP 200).
                // Photobooth category: cameras, props, party decorations.
                "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=200&h=200&fit=crop",
                "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=1200&h=600&fit=crop"),
            "safari-foodcourt" => (
                // Food court / casual dining imagery.
                "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=200&h=200&fit=crop",
                "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=1200&h=600&fit=crop"),
            _ => (
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=200&h=200&fit=crop",
                "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=1200&h=600&fit=crop"),
        };
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