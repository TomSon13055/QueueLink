using QueueLink.Models;

namespace QueueLink.ViewModels;

public class QueueCardViewModel
{
    public int QueueServiceId { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? VenueSlug { get; set; }
    public string? VenueLogoUrl { get; set; }
    public string? VenueCoverImageUrl { get; set; }
    public string VenueAddress { get; set; } = string.Empty;
    public int AvailableTableCount { get; set; }
    public int TotalTableCount { get; set; }
    public string QueueServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QueueStatus QueueStatus { get; set; }
    public int WaitingCount { get; set; }
    public int EstimatedWaitMinutes { get; set; }
}