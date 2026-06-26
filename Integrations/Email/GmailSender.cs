using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace QueueLink.Integrations.Email;

/// <summary>
/// SMTP implementation dùng MailKit. Mặc định cấu hình cho Gmail nhưng có thể trỏ
/// sang bất kỳ SMTP server nào (Outlook, SendGrid, ...) chỉ bằng cách đổi options.
/// </summary>
public class GmailSender : IEmailSender
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(15);

    private readonly EmailOptions _options;
    private readonly ILogger<GmailSender> _logger;

    public GmailSender(IOptions<EmailOptions> options, ILogger<GmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
        => SendCoreAsync(to, subject, body, isHtml: false, ct);

    public Task<bool> SendHtmlAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => SendCoreAsync(to, subject, htmlBody, isHtml: true, ct);

    private async Task<bool> SendCoreAsync(string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        // Fallback ra console nếu được cấu hình (dev mode hoặc chưa có Gmail).
        if (_options.UseConsoleFallback || string.IsNullOrWhiteSpace(_options.Smtp.Username))
        {
            _logger.LogWarning("[Email:ConsoleFallback] To={To} Subject={Subject}", to, subject);
            return true;
        }

        // Nếu SMTP host không resolve được → fail ngay (không đợi 15s timeout).
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(_options.Smtp.Host, ct);
            if (addresses.Length == 0)
            {
                _logger.LogError("[Email:Failed] Host '{Host}' could not be resolved", _options.Smtp.Host);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email:Failed] Could not resolve host '{Host}'", _options.Smtp.Host);
            return false;
        }

        try
        {
            using var client = new SmtpClient();

            client.Timeout = (int)SendTimeout.TotalMilliseconds;

            // Gmail yêu cầu STARTTLS trên cổng 587. Port 465 dùng SSL trực tiếp.
            var secure = _options.Smtp.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : (_options.Smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.StartTlsWhenAvailable);

            await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, secure, ct);
            await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.AppPassword, ct);

            var message = new MimeKit.MimeMessage();
            message.From.Add(MimeKit.MailboxAddress.Parse(_options.Smtp.From));
            message.To.Add(MimeKit.MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = isHtml
                ? new MimeKit.BodyBuilder { HtmlBody = body }.ToMessageBody()
                : new MimeKit.BodyBuilder { TextBody = body }.ToMessageBody();

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("[Email:Sent] To={To} Subject={Subject}", to, subject);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[Email:Cancelled] To={To} Subject={Subject} — request was cancelled", to, subject);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email:Failed] To={To} Subject={Subject}", to, subject);
            return false;
        }
    }
}
