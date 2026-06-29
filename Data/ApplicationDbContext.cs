using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QueueLink.Models;

namespace QueueLink.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<QueueService> QueueServices => Set<QueueService>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<VenueStaff> VenueStaff => Set<VenueStaff>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasIndex(u => u.NormalizedEmail).IsUnique();
        });

        builder.Entity<Venue>(e =>
        {
            e.HasIndex(v => v.Name);
            e.Property(v => v.Name).IsRequired();
            e.Property(v => v.Address).IsRequired();
            e.HasOne(v => v.Owner)
             .WithMany()
             .HasForeignKey(v => v.OwnerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<QueueService>(e =>
        {
            e.HasIndex(q => new { q.VenueId, q.Name });
            e.HasOne(q => q.Venue)
             .WithMany(v => v.QueueServices)
             .HasForeignKey(q => q.VenueId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QueueTicket>(e =>
        {
            e.HasIndex(t => new { t.QueueServiceId, t.TicketDate, t.TicketNumber }).IsUnique();
            e.HasIndex(t => t.PublicToken).IsUnique();

            e.HasOne(t => t.QueueService)
             .WithMany(q => q.Tickets)
             .HasForeignKey(t => t.QueueServiceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TicketStatusHistory>(e =>
        {
            e.HasOne(h => h.QueueTicket)
             .WithMany(t => t.StatusHistories)
             .HasForeignKey(h => h.QueueTicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CustomerProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.FullName).IsRequired();
            e.Property(p => p.Phone).IsRequired();
        });

        // ── Table ─────────────────────────────────────────────────────────
        builder.Entity<Table>(e =>
        {
            e.HasOne(t => t.Venue)
             .WithMany(v => v.Tables)
             .HasForeignKey(t => t.VenueId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.VenueId, t.Name }).IsUnique();
        });

        // ── Reservation ──────────────────────────────────────────────────
        builder.Entity<Reservation>(e =>
        {
            e.HasOne(r => r.Table)
             .WithMany(t => t.Reservations)
             .HasForeignKey(r => r.TableId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.ReservationCode).IsUnique();
            e.HasIndex(r => new { r.TableId, r.ReservationTime });
        });

        // ── MenuCategory ───────────────────────────────────────────────────
        builder.Entity<MenuCategory>(e =>
        {
            e.HasOne(m => m.Venue)
             .WithMany(v => v.MenuCategories)
             .HasForeignKey(m => m.VenueId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.VenueId, m.Name });
        });

        // ── MenuItem ──────────────────────────────────────────────────────
        builder.Entity<MenuItem>(e =>
        {
            e.HasOne(m => m.Category)
             .WithMany(c => c.Items)
             .HasForeignKey(m => m.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.CategoryId, m.Name });
        });

        // ── Order ─────────────────────────────────────────────────────────
        builder.Entity<Order>(e =>
        {
            e.HasOne(o => o.Table)
             .WithMany(t => t.Orders)
             .HasForeignKey(o => o.TableId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(o => o.OrderCode).IsUnique();
        });

        // ── OrderItem ─────────────────────────────────────────────────────
        builder.Entity<OrderItem>(e =>
        {
            e.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.MenuItem)
             .WithMany(m => m.OrderItems)
             .HasForeignKey(oi => oi.MenuItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payment ───────────────────────────────────────────────────────
        builder.Entity<Payment>(e =>
        {
            e.HasOne(p => p.Order)
             .WithMany(o => o.Payments)
             .HasForeignKey(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── VenueStaff ────────────────────────────────────────────────────
        builder.Entity<VenueStaff>(e =>
        {
            e.HasOne(vs => vs.Venue)
             .WithMany(v => v.StaffAssignments)
             .HasForeignKey(vs => vs.VenueId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(vs => vs.User)
             .WithMany()
             .HasForeignKey(vs => vs.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(vs => new { vs.VenueId, vs.UserId }).IsUnique();
        });
    }
}
