namespace QueueLink.ViewModels;

public class OwnerDashboardViewModel
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public List<TableDashboardViewModel> Tables { get; set; } = new();
    public List<ReservationListViewModel> TodayReservations { get; set; } = new();
    public List<QueueCardViewModel> QueueSummary { get; set; } = new();
}
