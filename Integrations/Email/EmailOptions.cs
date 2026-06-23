namespace QueueLink.Integrations.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Gmail";

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>
    /// Khi true, OTP và email sẽ được ghi ra log/console thay vì gửi qua SMTP.
    /// Bật trong development nếu chưa cấu hình Gmail.
    /// </summary>
    public bool UseConsoleFallback { get; set; } = false;
}

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;

    /// <summary>
    /// Địa chỉ Gmail dùng để gửi (vd: queuelink.notifier@gmail.com).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// App Password 16 ký tự tạo từ Google Account → Security → 2FA → App passwords.
    /// KHÔNG phải mật khẩu Gmail thường.
    /// </summary>
    public string AppPassword { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị + email (vd: "QueueLink <queuelink.notifier@gmail.com>").
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Bật SSL/TLS (true cho Gmail).
    /// </summary>
    public bool UseSsl { get; set; } = true;
}
