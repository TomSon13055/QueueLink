using System.ComponentModel.DataAnnotations;
using QueueLink.Models;

namespace QueueLink.ViewModels;

public class QueueServiceFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Địa điểm")]
    public int VenueId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên hàng chờ")]
    [StringLength(150)]
    [Display(Name = "Tên hàng chờ")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập prefix (1-5 ký tự)")]
    [StringLength(5, MinimumLength = 1)]
    [Display(Name = "Prefix (VD: A, P, T)")]
    public string Prefix { get; set; } = "A";

    [Range(1, 240)]
    [Display(Name = "Thời gian phục vụ trung bình (phút)")]
    public int AverageServiceMinutes { get; set; } = 5;

    [Display(Name = "Trạng thái")]
    public QueueStatus QueueStatus { get; set; } = QueueStatus.Open;

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public string? VenueName { get; set; }
}