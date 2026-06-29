using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public enum PaymentMethod
{
    Cash = 0,
    PayOS = 1,
    BankTransfer = 2,
    Momo = 3,
    Other = 99
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>
    /// Số tiền thanh toán (VND)
    /// </summary>
    public decimal Amount { get; set; } = 0;

    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Mã giao dịch từ PayOS / momo / bank
    /// </summary>
    [StringLength(100)]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Link thanh toán PayOS (nếu dùng PayOS)
    /// </summary>
    [StringLength(500)]
    public string? PaymentUrl { get; set; }

    /// <summary>
    /// Ghi chú thanh toán
    /// </summary>
    [StringLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Staff xử lý thanh toán
    /// </summary>
    [StringLength(450)]
    public string? ProcessedByStaffId { get; set; }
}
