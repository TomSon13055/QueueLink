using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

/// <summary>
/// Thông tin bổ sung cho khách đã đăng ký. Liên kết 1-1 với ApplicationUser.
/// Dùng để auto-fill form lấy số và gửi email thông báo.
/// </summary>
public class CustomerProfile
{
    public int Id { get; set; }

    /// <summary>
    /// ApplicationUser.Id. Mỗi user chỉ có 1 profile.
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
