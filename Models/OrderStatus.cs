using System.ComponentModel.DataAnnotations;

namespace QueueLink.Models;

public enum OrderStatus
{
    Open = 0,       // Đang mở / đang gọi món
    Submitted = 1,  // Đã gửi bếp
    Served = 2,     // Đã ra món đủ
    Paid = 3,       // Đã thanh toán
    Cancelled = 4   // Đã hủy
}
