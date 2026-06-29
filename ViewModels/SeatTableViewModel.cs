namespace QueueLink.ViewModels;

public class SeatTableViewModel
{
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; } = 1;
    public string? Notes { get; set; }
}
