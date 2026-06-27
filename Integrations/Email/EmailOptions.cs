namespace QueueLink.Integrations.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// "Gmail" hoặc "Resend". Khi là "Resend", dùng HTTP API thay vì SMTP.
    /// </summary>
    public string Provider { get; set; } = "Gmail";

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>
    /// Khi true, OTP và email sẽ được ghi ra log/console thay vì gửi qua SMTP.
    /// Bật trong development nếu chưa cấu hình Gmail.
    /// </summary>
    public bool UseConsoleFallback { get; set; } = false;

    public ResendOptions Resend { get; set; } = new();
    public BrevoOptions Brevo { get; set; } = new();
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

public class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string FromName { get; set; } = "QueueLink";
}

public class BrevoOptions
{
    public const string SectionName = "Brevo";

    public string ApiKey { get; set; } = string.Empty;
    public string SenderName { get; set; } = "QueueLink";
}
