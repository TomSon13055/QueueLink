using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class TableFormViewModel
{
    public int Id { get; set; }
    public int VenueId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên bàn")]
    [StringLength(50)]
    [Display(Name = "Tên bàn")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Sức chứa từ 1 đến 100")]
    [Display(Name = "Sức chứa (người)")]
    public int Capacity { get; set; } = 4;

    public QueueLink.Models.TableStatus Status { get; set; } = QueueLink.Models.TableStatus.Available;

    [Display(Name = "Thứ tự hiển thị")]
    public int SortOrder { get; set; } = 0;

    [Display(Name = "Hoạt động")]
    public bool IsActive { get; set; } = true;
}
