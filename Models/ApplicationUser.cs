using Microsoft.AspNetCore.Identity;

namespace QueueLink.Models;

public class ApplicationUser : IdentityUser
{
    [System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}