using System.Text.Json;
using QueueLink.Integrations.Session;

namespace QueueLink.Integrations.Session;

/// <summary>
/// Helper quản lý session: đọc/ghi thông tin guest profile dùng để auto-fill form lấy số.
/// </summary>
public interface IGuestSession
{
    GuestProfile? Get();
    void Set(GuestProfile profile);
    void Clear();
}

public class GuestSession : IGuestSession
{
    private readonly IHttpContextAccessor _accessor;

    public GuestSession(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession? Session => _accessor.HttpContext?.Session;

    public GuestProfile? Get()
    {
        var json = Session?.GetString(SessionKeys.GuestProfile);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<GuestProfile>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Set(GuestProfile profile)
    {
        Session?.SetString(SessionKeys.GuestProfile, JsonSerializer.Serialize(profile));
    }

    public void Clear()
    {
        Session?.Remove(SessionKeys.GuestProfile);
    }
}
