using QueueLink.Models;

namespace QueueLink.ViewModels;

public class TableDashboardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public TableStatus Status { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public Reservation? ActiveReservation { get; set; }
    public Order? ActiveOrder { get; set; }
}
