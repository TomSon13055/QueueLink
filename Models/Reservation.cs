using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class Reservation
{
    public int Id { get; set; }

    public int TableId { get; set; }
    public Table? Table { get; set; }

    public int? VenueId { get; set; }
    public Venue? Venue { get; set; }

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    public int PartySize { get; set; } = 1;

    /// <summary>
    /// Thời gian khách muốn đến (ngày + giờ đặt)
    /// </summary>
    public DateTime ReservationTime { get; set; }

    /// <summary>
    /// Thời gian giữ bàn tối đa (ReservationTime + HoldMinutes)
    /// Hết giờ mà khách chưa đến → bàn tự động về Available
    /// </summary>
    public int HoldMinutes { get; set; } = 30;

    /// <summary>
    /// Giờ hết hạn giữ bàn — computed: ReservationTime.AddMinutes(HoldMinutes)
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime ExpiresAt => ReservationTime.AddMinutes(HoldMinutes);

    /// <summary>
    /// Mã đặt trước hiển thị cho khách, ví dụ: RES-7823
    /// </summary>
    [StringLength(20)]
    public string? ReservationCode { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Nếu khách có tài khoản
    /// </summary>
    [StringLength(450)]
    public string? UserId { get; set; }
}
