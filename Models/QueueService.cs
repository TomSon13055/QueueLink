using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class QueueService
{
    public int Id { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    // Short prefix used to build the public ticket code, e.g. "A001", "P001"
    [Required]
    [StringLength(5, MinimumLength = 1)]
    public string Prefix { get; set; } = "A";

    public int AverageServiceMinutes { get; set; } = 5;

    public QueueStatus QueueStatus { get; set; } = QueueStatus.Open;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QueueTicket> Tickets { get; set; } = new List<QueueTicket>();
}