using Microsoft.AspNetCore.Identity;

namespace QueueLink.Models;

public class ApplicationUser : IdentityUser
{
    [System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Supabase Auth user ID. Null = local-only account.</summary>
    public string? SupabaseId { get; set; }

    /// <summary>Provider used for this account (Local, Google, etc.).</summary>
    public string? Provider { get; set; }
}