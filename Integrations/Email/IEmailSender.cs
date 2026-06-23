namespace QueueLink.Integrations.Email;

/// <summary>
/// Abstraction cho mọi provider email (Gmail, SendGrid, mock console, ...).
/// Controller/service chỉ phụ thuộc interface này.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Gửi email text thuần (subject + body). Trả về true nếu gửi thành công.
    /// </summary>
    Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);

    /// <summary>
    /// Gửi email HTML. Trả về true nếu gửi thành công.
    /// </summary>
    Task<bool> SendHtmlAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
