using System.ComponentModel.DataAnnotations;

using QueueLink.Models;

namespace QueueLink.ViewModels;

public class OrderViewModel
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int VenueId { get; set; }
    public string? OrderCode { get; set; }
    public int PartySize { get; set; } = 1;
    public string? CustomerName { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = new();
}

public class OrderItemViewModel
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public string? Notes { get; set; }
    public bool IsServed { get; set; }
}
