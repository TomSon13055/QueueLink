using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

/// <summary>
/// Gán nhân viên (Staff) vào Venue để xác định quyền truy cập.
/// Mỗi Staff có thể được assign vào nhiều Venue.
/// </summary>
public class VenueStaff
{
    public int Id { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
