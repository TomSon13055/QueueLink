using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QueueLink.Data;
using QueueLink.Models;

namespace QueueLink.Services;

public static class SeedData
{
    // Hardcoded: must match the latest migration class name.
    // Update this if you add/rename migrations.
    private const string CurrentMigrationName = "AddVenueCoverImageUrl";

    // All migrations in chronological order. Used for baseline detection.
    private static readonly string[] AllMigrations = new[]
    {
        "20260626131810_InitialCreate",
        "20260627033525_AddDataProtectionKeys",
        "20260628172948_RestaurantModule",
        "20260630033015_AddVenueCoverImageUrl",
    };

    public static async Task InitializeAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await ApplyMigrationsSafelyAsync(db);

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

    /// <summary>
    /// Appending safely handles three Railway scenarios:
    ///   A) Brand-new DB → MigrateAsync runs normally.
    ///   B) DB has tables but no migration history (old deploy, partial state) →
    ///      baseline the history so EF skips already-applied migrations.
    ///   C) A migration partially applied → recover column-wise, then complete it.
    /// </summary>
    private static async Task ApplyMigrationsSafelyAsync(ApplicationDbContext db)
    {
        // 1. Probe the DB to learn its actual state.
        var state = await ProbeDatabaseStateAsync(db);

        // 2. If history is missing/empty but tables already exist, baseline.
        if (state.HasTables && state.HistoryRows == 0)
        {
            await BaselineHistoryAsync(db, state);
        }

        // 3. Try MigrateAsync. If it still hits a recoverable error,
        //    patch and retry once.
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var pg = GetPgException(ex);
            if (pg == null) throw;

            // 42703 = undefined_column, 42P07 = duplicate_table,
            // 42710 = duplicate_object (e.g. duplicate index/constraint),
            // 42P01 = undefined_table (e.g. DROP TABLE on missing table).
            if (pg.SqlState is "42703" or "42P07" or "42710" or "42P01")
            {
                await RecoverFromPartialMigrationAsync(db, pg);
                // Mark the migration that was being attempted as applied,
                // so EF doesn't try to run it again on the next request.
                await MarkLatestMigrationAppliedAsync(db);
            }
            else
            {
                throw;
            }
        }
    }

    private record DbState(bool HasTables, bool HasHistoryTable, int HistoryRows);

    private static async Task<DbState> ProbeDatabaseStateAsync(ApplicationDbContext db)
    {
        // Conn is the underlying ADO connection so we don't depend on the model.
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();

        // Does any expected table exist? Use AspNetUsers as the canary —
        // it's the first thing InitialCreate creates.
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_name = 'AspNetUsers';";
        var hasTablesObj = await cmd.ExecuteScalarAsync();
        var hasTables = Convert.ToInt32(hasTablesObj) > 0;

        // Does __EFMigrationsHistory exist?
        cmd.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory';";
        var hasHistoryObj = await cmd.ExecuteScalarAsync();
        var hasHistory = Convert.ToInt32(hasHistoryObj) > 0;

        int historyRows = 0;
        if (hasHistory)
        {
            cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";";
            var rowsObj = await cmd.ExecuteScalarAsync();
            historyRows = Convert.ToInt32(rowsObj ?? 0);
        }

        return new DbState(hasTables, hasHistory, historyRows);
    }

    /// <summary>
    /// Insert synthetic rows into __EFMigrationsHistory for any migration
    /// whose tables we can already observe in the live schema. This makes
    /// EF treat them as applied without re-running them.
    /// </summary>
    private static async Task BaselineHistoryAsync(ApplicationDbContext db, DbState state)
    {
        // Make sure the history table itself exists.
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId""    character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32)  NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );");

        var applied = await GetAppliedMigrationNamesAsync(db);
        var version = "9.0.0";

        foreach (var mig in AllMigrations)
        {
            if (applied.Contains(mig)) continue;
            // Values come from compile-time constants (AllMigrations, "9.0.0"),
            // never user input, so interpolation here is safe.
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                   VALUES ('{mig}', '{version}')
                   ON CONFLICT (""MigrationId"") DO NOTHING;");
#pragma warning restore EF1002
        }
    }

    private static async Task<HashSet<string>> GetAppliedMigrationNamesAsync(ApplicationDbContext db)
    {
        var set = new HashSet<string>();
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            set.Add(reader.GetString(0));
        return set;
    }

    /// <summary>
    /// Best-effort patch for whatever step the running migration was on when
    /// it failed. We just create whatever's missing idempotently and let
    /// the caller mark the migration as applied.
    /// </summary>
    private static async Task RecoverFromPartialMigrationAsync(ApplicationDbContext db, Npgsql.PostgresException pg)
    {
        // Venues columns added by RestaurantModule + AddVenueCoverImageUrl.
        await db.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""CloseTime""     time without time zone NOT NULL DEFAULT '00:00:00';
            ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""OpenTime""      time without time zone NOT NULL DEFAULT '00:00:00';
            ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""OwnerId""       character varying(450);
            ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""Slug""          character varying(200);
            ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""CoverImageUrl"" character varying(500);
        ");

        // Tables created by RestaurantModule. IF NOT EXISTS makes this safe
        // whether the table is missing (we need it) or already present
        // (42P07 case from a half-applied run).
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Tables"" (
                ""Id""        integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""VenueId""   integer NOT NULL,
                ""Name""      character varying(50)  NOT NULL,
                ""Capacity""  integer NOT NULL,
                ""Status""    integer NOT NULL,
                ""IsActive""  boolean NOT NULL,
                ""SortOrder"" integer NOT NULL,
                CONSTRAINT ""PK_Tables"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""MenuCategories"" (
                ""Id""        integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""VenueId""   integer NOT NULL,
                ""Name""      character varying(100) NOT NULL,
                ""SortOrder"" integer NOT NULL,
                ""IsActive""  boolean NOT NULL,
                CONSTRAINT ""PK_MenuCategories"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""VenueStaff"" (
                ""Id""         integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""VenueId""    integer NOT NULL,
                ""UserId""     character varying(450) NOT NULL,
                ""AssignedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_VenueStaff"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""MenuItems"" (
                ""Id""          integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""CategoryId""  integer NOT NULL,
                ""VenueId""     integer,
                ""Name""        character varying(200) NOT NULL,
                ""Description"" character varying(1000),
                ""Price""       numeric NOT NULL,
                ""ImageUrl""    character varying(500),
                ""IsActive""    boolean NOT NULL,
                ""IsAvailable"" boolean NOT NULL,
                ""SortOrder""   integer NOT NULL,
                CONSTRAINT ""PK_MenuItems"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""Orders"" (
                ""Id""           integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""TableId""      integer NOT NULL,
                ""VenueId""      integer NOT NULL,
                ""OrderCode""    character varying(20),
                ""PartySize""    integer NOT NULL,
                ""CustomerName"" character varying(120),
                ""Status""       integer NOT NULL,
                ""SubTotal""     numeric NOT NULL,
                ""TaxAmount""    numeric NOT NULL,
                ""DiscountAmount"" numeric NOT NULL,
                ""TotalAmount""  numeric NOT NULL,
                ""Notes""        character varying(500),
                ""CreatedAt""    timestamp with time zone NOT NULL,
                ""SubmittedAt""  timestamp with time zone,
                ""PaidAt""       timestamp with time zone,
                ""StaffId""      character varying(450),
                ""ReservationId"" integer,
                CONSTRAINT ""PK_Orders"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""Reservations"" (
                ""Id""               integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""TableId""          integer NOT NULL,
                ""VenueId""          integer,
                ""CustomerName""     character varying(120) NOT NULL,
                ""CustomerPhone""    character varying(30)  NOT NULL,
                ""PartySize""        integer NOT NULL,
                ""ReservationTime""  timestamp with time zone NOT NULL,
                ""HoldMinutes""      integer NOT NULL,
                ""ReservationCode""  character varying(20),
                ""Notes""            character varying(500),
                ""Status""           integer NOT NULL,
                ""CreatedAt""        timestamp with time zone NOT NULL,
                ""ConfirmedAt""      timestamp with time zone,
                ""SeatedAt""         timestamp with time zone,
                ""CancelledAt""      timestamp with time zone,
                ""UserId""           character varying(450),
                CONSTRAINT ""PK_Reservations"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""OrderItems"" (
                ""Id""         integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""OrderId""    integer NOT NULL,
                ""MenuItemId"" integer NOT NULL,
                ""ItemName""   character varying(200) NOT NULL,
                ""Quantity""   integer NOT NULL,
                ""UnitPrice""  numeric NOT NULL,
                ""Notes""      character varying(300),
                ""IsServed""   boolean NOT NULL,
                ""CreatedAt""  timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_OrderItems"" PRIMARY KEY (""Id"")
            );
            CREATE TABLE IF NOT EXISTS ""Payments"" (
                ""Id""               integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                ""OrderId""          integer NOT NULL,
                ""Amount""           numeric NOT NULL,
                ""Method""           integer NOT NULL,
                ""Status""           integer NOT NULL,
                ""TransactionId""    character varying(100),
                ""PaymentUrl""       character varying(500),
                ""Notes""            character varying(300),
                ""CreatedAt""        timestamp with time zone NOT NULL,
                ""PaidAt""           timestamp with time zone,
                ""ProcessedByStaffId"" character varying(450),
                CONSTRAINT ""PK_Payments"" PRIMARY KEY (""Id"")
            );
        ");
    }

    private static async Task MarkLatestMigrationAppliedAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId""    character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32)  NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );");

        await db.Database.ExecuteSqlRawAsync($@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('{CurrentMigrationName}', '9.0.0')
            ON CONFLICT (""MigrationId"") DO NOTHING;");
    }

    private static Npgsql.PostgresException? GetPgException(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is Npgsql.PostgresException pg) return pg;
            ex = ex.InnerException;
        }
        return null;
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