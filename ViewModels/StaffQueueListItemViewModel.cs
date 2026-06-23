namespace QueueLink.ViewModels;

public class StaffQueueListItemViewModel
{
    public int QueueServiceId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string QueueServiceName { get; set; } = string.Empty;
    public Models.QueueStatus QueueStatus { get; set; }
    public int WaitingCount { get; set; }
    public string? CurrentCallingTicketCode { get; set; }
}