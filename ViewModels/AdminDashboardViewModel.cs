namespace QueueLink.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalTicketsToday { get; set; }
    public int WaitingTickets { get; set; }
    public int CompletedTickets { get; set; }
    public int NoShowTickets { get; set; }
    public int CancelledTickets { get; set; }
    public double AverageEstimatedWaitMinutes { get; set; }

    public int TotalVenues { get; set; }
    public int TotalQueueServices { get; set; }
    public int OpenQueues { get; set; }
}