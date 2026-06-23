namespace QueueLink.ViewModels;

public class QueueServiceDetailsViewModel
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Prefix { get; set; } = "A";
    public int AverageServiceMinutes { get; set; }
    public Models.QueueStatus QueueStatus { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PublicJoinUrl { get; set; } = string.Empty;
    public int WaitingCount { get; set; }
}