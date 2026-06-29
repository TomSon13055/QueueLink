using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [Required]
    [StringLength(200)]
    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Giá tại thời điểm đặt (phòng giá thay đổi)
    /// </summary>
    public decimal UnitPrice { get; set; } = 0;

    public decimal TotalPrice => Quantity * UnitPrice;

    /// <summary>
    /// Ghi chú riêng cho món này (vd: "ít đường", "thêm phô mai")
    /// </summary>
    [StringLength(300)]
    public string? Notes { get; set; }

    public bool IsServed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
