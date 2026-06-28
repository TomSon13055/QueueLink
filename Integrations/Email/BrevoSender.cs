using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace QueueLink.Integrations.Email;

/// <summary>
/// Gửi email qua Brevo (brevo.com) SMTP HTTP API.
/// Không cần domain riêng — miễn phí 300 email/ngày, gửi được tới mọi email.
/// </summary>
public class BrevoSender : IEmailSender
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.brevo.com"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly EmailOptions _options;
    private readonly ILogger<BrevoSender> _logger;

    public BrevoSender(IOptions<EmailOptions> options, ILogger<BrevoSender> logger)
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
        if (string.IsNullOrWhiteSpace(_options.Brevo.ApiKey))
        {
            _logger.LogWarning("[Email:Brevo] API key not configured. Falling back to console.");
            _logger.LogWarning("[Email:ConsoleFallback] To={To} Subject={Subject}", to, subject);
            return true;
        }

        var payload = new
        {
            sender = new { name = _options.Brevo.SenderName, email = _options.Brevo.SenderEmail },
            to = new[] { new { email = to } },
            subject,
            htmlContent = isHtml ? body : $"<p>{body}</p>",
            text = isHtml ? null : body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v3/smtp/email");
        request.Headers.Add("api-key", _options.Brevo.ApiKey);
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
            _logger.LogError(ex, "[Email:Brevo] Request failed for {To}", to);
            return false;
        }

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("[Email:Brevo] Sent to={To} Subject={Subject}", to, subject);
            return true;
        }

        var body_resp = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("[Email:Brevo] Failed status={Status} body={Body}", response.StatusCode, body_resp);
        return false;
    }
}
