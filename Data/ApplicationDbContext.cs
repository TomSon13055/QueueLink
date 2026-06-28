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
    }
}
