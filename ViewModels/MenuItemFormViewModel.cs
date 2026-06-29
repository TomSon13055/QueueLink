using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class MenuItemFormViewModel
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public int CategoryId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Tên món")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(0, 100_000_000)]
    [Display(Name = "Giá (VND)")]
    public decimal Price { get; set; }

    [StringLength(500)]
    [Display(Name = "URL ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Hoạt động")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Còn món")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Thứ tự")]
    public int SortOrder { get; set; }
}
