using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class ReservationViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(120)]
    [Display(Name = "Họ tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(30)]
    [Phone]
    [Display(Name = "Số điện thoại")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Range(1, 50)]
    [Display(Name = "Số khách")]
    public int PartySize { get; set; } = 2;

    [Required(ErrorMessage = "Vui lòng chọn ngày đặt")]
    [DataType(DataType.Date)]
    public DateTime ReservationDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn giờ đặt")]
    [DataType(DataType.Time)]
    public TimeOnly ReservationTime { get; set; } = new(18, 0);

    public int TableId { get; set; }
    public string? TableName { get; set; }

    [Display(Name = "Ghi chú")]
    [StringLength(500)]
    public string? Notes { get; set; }

    public int HoldMinutes { get; set; } = 30;
}
