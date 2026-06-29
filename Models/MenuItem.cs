using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class MenuItem
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public MenuCategory? Category { get; set; }

    public int? VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Giá bán, đơn vị VND. 0 = theo yêu cầu / không bán online
    /// </summary>
    public decimal Price { get; set; } = 0;

    /// <summary>
    /// URL ảnh món ăn
    /// </summary>
    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAvailable { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
