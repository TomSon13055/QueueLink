namespace QueueLink.ViewModels;

public class QueueSummaryDto
{
    public int QueueServiceId { get; set; }
    public int WaitingCount { get; set; }
    public int AverageEstimatedWaitMinutes { get; set; }
    public string? CurrentCallingTicketCode { get; set; }
    public Models.QueueStatus QueueStatus { get; set; }
}