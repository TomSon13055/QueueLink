using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

/// <summary>
/// Lưu OTP xác thực email. Một email có thể có nhiều OTP theo thời gian,
/// chỉ cái chưa dùng + còn hạn mới hợp lệ.
/// </summary>
public class EmailOtp
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mã OTP đã hash (SHA256). Không lưu plain text để tăng bảo mật.
    /// </summary>
    [Required]
    [StringLength(128)]
    public string OtpHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Số lần đã gửi lại (resend) — giới hạn để chống spam.
    /// </summary>
    public int AttemptCount { get; set; }
}
