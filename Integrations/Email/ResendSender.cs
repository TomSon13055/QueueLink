using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace QueueLink.Integrations.Email;

/// <summary>
/// Gửi email qua Resend HTTP API (resend.com). Không cần mở cổng SMTP,
/// chỉ cần HTTPS ra ngoài — hoạt động tốt trên Railway/Render.
/// </summary>
public class ResendSender : IEmailSender
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.resend.com"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly EmailOptions _options;
    private readonly ILogger<ResendSender> _logger;

    public ResendSender(IOptions<EmailOptions> options, ILogger<ResendSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
        => await SendCoreAsync(to, subject, body, false, ct);

    public async Task<bool> SendHtmlAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => await SendCoreAsync(to, subject, htmlBody, true, ct);

    private async Task<bool> SendCoreAsync(string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Resend.ApiKey) || string.IsNullOrWhiteSpace(_options.Resend.Domain))
        {
            _logger.LogWarning("[Email:Resend] API key or domain not configured. Falling back to console.");
            _logger.LogWarning("[Email:ConsoleFallback] To={To} Subject={Subject}", to, subject);
            return true;
        }

        var payload = new
        {
            from = $"{_options.Resend.FromName} <onboarding@{_options.Resend.Domain}>",
            to = new[] { to },
            subject,
            html = isHtml ? body : null,
            text = isHtml ? null : body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/emails");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Resend.ApiKey);
        request.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email:Resend] Request failed for {To}", to);
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[Email:Resend] Sent to={To} Subject={Subject}", to, subject);
            return true;
        }

        var body_resp = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("[Email:Resend] Failed status={Status} body={Body}", response.StatusCode, body_resp);
        return false;
    }
}
