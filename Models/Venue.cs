using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class Venue
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// URL slug cho trang công khai, ví dụ: "dookki-nguyen-trai"
    /// </summary>
    [StringLength(200)]
    public string? Slug { get; set; }

    /// <summary>
    /// Chủ sở hữu quán — ApplicationUser.Id
    /// </summary>
    [StringLength(450)]
    public string? OwnerId { get; set; }

    public ApplicationUser? Owner { get; set; }

    /// <summary>
    /// Giờ mở cửa trong ngày, ví dụ: 12:00
    /// </summary>
    public TimeOnly OpenTime { get; set; } = new TimeOnly(12, 0);

    /// <summary>
    /// Giờ đóng cửa trong ngày, ví dụ: 22:00
    /// </summary>
    public TimeOnly CloseTime { get; set; } = new TimeOnly(22, 0);

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QueueService> QueueServices { get; set; } = new List<QueueService>();
    public ICollection<Table> Tables { get; set; } = new List<Table>();
    public ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();
    public ICollection<VenueStaff> StaffAssignments { get; set; } = new List<VenueStaff>();
}