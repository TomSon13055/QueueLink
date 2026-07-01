using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueueLink.Models;

public class Table
{
    public int Id { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; } = 4;

    public TableStatus Status { get; set; } = TableStatus.Available;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    // ── Floor-plan layout (not mapped — managed via raw SQL) ──────────
    // EF is told to ignore these so queries never SELECT them.
    // The actual PostgreSQL columns are created / updated by raw SQL
    // in Program.cs (initial migration) and in the controller endpoints
    // (drag-drop saves / TableCreate / TableEdit).

    [NotMapped]
    public decimal LayoutX { get; set; } = 50m;

    [NotMapped]
    public decimal LayoutY { get; set; } = 50m;

    [NotMapped]
    public decimal LayoutW { get; set; } = 12m;

    [NotMapped]
    public decimal LayoutH { get; set; } = 9m;

    [NotMapped]
    [StringLength(50)]
    public string? Block { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
