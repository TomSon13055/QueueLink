using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class VenueFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên địa điểm")]
    [StringLength(200)]
    [Display(Name = "Tên địa điểm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [StringLength(300)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [StringLength(30)]
    [Phone]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [StringLength(500)]
    [Url]
    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    [Url]
    [Display(Name = "Cover Image URL (ảnh bìa)")]
    public string? CoverImageUrl { get; set; }

    [StringLength(200)]
    [Display(Name = "Slug (URL công khai)")]
    public string? Slug { get; set; }

    [Display(Name = "Giờ mở cửa")]
    public TimeOnly OpenTime { get; set; } = new(11, 0);

    [Display(Name = "Giờ đóng cửa")]
    public TimeOnly CloseTime { get; set; } = new(22, 0);

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}