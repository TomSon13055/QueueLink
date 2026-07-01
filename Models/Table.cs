using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Position on the floor-plan canvas (percent of canvas width).
    /// 0..100. Persisted as decimal so hosts can place tables precisely.
    /// </summary>
    public decimal LayoutX { get; set; } = 50m;
    public decimal LayoutY { get; set; } = 50m;
    public decimal LayoutW { get; set; } = 12m;
    public decimal LayoutH { get; set; } = 9m;

    /// <summary>
    /// Optional grouping label so hosts can arrange tables into
    /// blocks / zones (e.g. "VIP", "Outdoor", "Patio"). Tables in
    /// the same block are listed together in the editor and
    /// rendered under the same block heading on the public plan.
    /// </summary>
    [StringLength(50)]
    public string? Block { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
