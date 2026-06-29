using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class Order
{
    public int Id { get; set; }

    public int TableId { get; set; }
    public Table? Table { get; set; }

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    /// <summary>
    /// Mã hóa đơn hiển thị, ví dụ: ORD-001234
    /// </summary>
    [StringLength(20)]
    public string? OrderCode { get; set; }

    public int PartySize { get; set; } = 1;

    /// <summary>
    /// Tên khách (nếu khách không đăng nhập)
    /// </summary>
    [StringLength(120)]
    public string? CustomerName { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Open;

    public decimal SubTotal { get; set; } = 0;
    public decimal TaxAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;

    /// <summary>
    /// Ghi chú cho toàn đơn hàng
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Staff tạo đơn
    /// </summary>
    [StringLength(450)]
    public string? StaffId { get; set; }

    /// <summary>
    /// Mã reservation liên quan (nếu có)
    /// </summary>
    public int? ReservationId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
