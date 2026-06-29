namespace QueueLink.Models;

public enum TableStatus
{
    Available = 0,   // Trống, sẵn sàng
    Reserved = 1,    // Đặt trước
    Occupied = 2,    // Đang có khách
    Cleaning = 3     // Đang dọn dẹp
}
