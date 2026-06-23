using QueueLink.Models;

namespace QueueLink.ViewModels;

public class StaffQueueDashboardViewModel
{
    public int QueueServiceId { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string QueueServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QueueStatus QueueStatus { get; set; }
    public int WaitingCount { get; set; }
    public int AverageEstimatedWaitMinutes { get; set; }
    public string? CurrentCallingTicketCode { get; set; }
    public List<StaffTicketRowViewModel> Tickets { get; set; } = new();
}