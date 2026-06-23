using System.ComponentModel.DataAnnotations;

namespace QueueLink.ViewModels;

public class JoinQueueViewModel
{
    public int QueueServiceId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
    [StringLength(120, MinimumLength = 1)]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(30)]
    [Display(Name = "Số điện thoại")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Số lượng khách phải từ 1 đến 20")]
    [Display(Name = "Số lượng khách")]
    public int PartySize { get; set; } = 1;

    // Auxiliary display data populated by the controller, not bound from the form.
    public string? VenueName { get; set; }
    public string? QueueServiceName { get; set; }
    public string? Prefix { get; set; }
}