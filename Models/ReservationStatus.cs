namespace QueueLink.Models;

public enum ReservationStatus
{
    Pending = 0,     // Chờ xác nhận
    Confirmed = 1,   // Đã xác nhận
    Seated = 2,      // Đã ngồi vào bàn
    Completed = 3,   // Hoàn thành
    Cancelled = 4,   // Đã hủy
    NoShow = 5       // Không đến
}
