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

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
