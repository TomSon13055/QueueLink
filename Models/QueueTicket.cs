using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class QueueTicket
{
    public int Id { get; set; }

    public int QueueServiceId { get; set; }
    public QueueService? QueueService { get; set; }

    // The per-queue, per-day sequential number used to generate TicketCode.
    public int TicketNumber { get; set; }

    // The full public ticket code, e.g. "A001".
    [Required]
    [StringLength(20)]
    public string TicketCode { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string CustomerPhone { get; set; } = string.Empty;

    public int PartySize { get; set; } = 1;

    public TicketStatus Status { get; set; } = TicketStatus.Waiting;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The date component used for the unique (QueueServiceId, TicketDate, TicketNumber) constraint.
    public DateTime TicketDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime? CalledAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public int EstimatedWaitMinutes { get; set; }

    /// <summary>
    /// Nếu khách đăng nhập khi lấy số, liên kết ticket với ApplicationUser.Id
    /// để auto-fill hồ sơ và gửi email thông báo.
    /// </summary>
    [StringLength(450)]
    public string? UserId { get; set; }

    [Required]
    [StringLength(64)]
    public string PublicToken { get; set; } = string.Empty;

    public ICollection<TicketStatusHistory> StatusHistories { get; set; } = new List<TicketStatusHistory>();
}