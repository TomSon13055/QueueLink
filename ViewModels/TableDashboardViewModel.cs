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

    // Layout on the floor-plan canvas (percent of canvas size).
    public decimal LayoutX { get; set; }
    public decimal LayoutY { get; set; }
    public decimal LayoutW { get; set; }
    public decimal LayoutH { get; set; }

    // Optional grouping (VIP, Outdoor, …).
    public string? Block { get; set; }

    public Reservation? ActiveReservation { get; set; }
    public Order? ActiveOrder { get; set; }
}
