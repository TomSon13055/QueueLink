using QueueLink.Models;

namespace QueueLink.ViewModels;

public class StaffVenueAssignmentViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public int? AssignmentId { get; set; }
}
