using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class TicketStatusHistory
{
    public int Id { get; set; }

    public int QueueTicketId { get; set; }
    public QueueTicket? QueueTicket { get; set; }

    [StringLength(20)]
    public string? OldStatus { get; set; }

    [Required]
    [StringLength(20)]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? ChangedByUserId { get; set; }
}