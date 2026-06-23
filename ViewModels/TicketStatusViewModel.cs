using QueueLink.Models;

namespace QueueLink.ViewModels;

public class TicketStatusViewModel
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string QueueServiceName { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int PeopleAhead { get; set; }
    public int EstimatedWaitMinutes { get; set; }
    public string? CurrentCallingTicketCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PublicToken { get; set; } = string.Empty;
    public int QueueServiceId { get; set; }
    public DateTime CreatedAt { get; set; }
}