using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QueueLink.Models;

namespace QueueLink.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<QueueService> QueueServices => Set<QueueService>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
            // Unique constraint: (QueueServiceId, TicketDate, TicketNumber)
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

        builder.Entity<EmailOtp>(e =>
        {
            e.HasIndex(o => new { o.Email, o.IsUsed, o.ExpiresAt });
            e.Property(o => o.Email).IsRequired();
        });

        builder.Entity<CustomerProfile>(e =>
        {
            // 1-1 với ApplicationUser
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