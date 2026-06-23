namespace QueueLink.Integrations.Session;

public static class SessionKeys
{
    /// <summary>
    /// Lưu thông tin khách đã đăng ký/đăng nhập để auto-fill form lấy số.
    /// Value: JSON của <see cref="GuestProfile"/>.
    /// </summary>
    public const string GuestProfile = "GuestProfile";
}

public class GuestProfile
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
