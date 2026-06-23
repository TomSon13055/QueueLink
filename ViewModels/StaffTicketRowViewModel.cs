using QueueLink.Models;

namespace QueueLink.ViewModels;

public class StaffTicketRowViewModel
{
    public int TicketId { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public DateTime CreatedAt { get; set; }
    public TicketStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int EstimatedWaitMinutes { get; set; }
    public string PublicToken { get; set; } = string.Empty;
}