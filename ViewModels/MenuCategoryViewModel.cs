using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class MenuCategoryViewModel
{
    public int Id { get; set; }
    public int VenueId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự")]
    public int SortOrder { get; set; }

    [Display(Name = "Hoạt động")]
    public bool IsActive { get; set; } = true;
}
