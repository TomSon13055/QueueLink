using QueueLink.Models;

namespace QueueLink.ViewModels;

public class ReservationListViewModel
{
    public int Id { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public DateTime ReservationTime { get; set; }
    public int HoldMinutes { get; set; }
    public DateTime ExpiresAt => ReservationTime.AddMinutes(HoldMinutes);
    public ReservationStatus Status { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
