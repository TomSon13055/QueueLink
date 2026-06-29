using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class MenuCategory
{
    public int Id { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
