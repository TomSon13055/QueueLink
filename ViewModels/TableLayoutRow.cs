namespace QueueLink.ViewModels;

/// <summary>
/// Projection for reading layout columns from the raw "Tables" table.
/// These columns are [NotMapped] on the Table entity and are
/// managed exclusively through raw SQL.
/// </summary>
public class TableLayoutRow
{
    public int Id { get; set; }
    public decimal LayoutX { get; set; }
    public decimal LayoutY { get; set; }
    public decimal LayoutW { get; set; }
    public decimal LayoutH { get; set; }
    public string? Block { get; set; }
}
