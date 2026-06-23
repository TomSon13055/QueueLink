namespace QueueLink.Integrations.Email;

public static class EmailTemplates
{
    /// <summary>
    /// Email xác thực tài khoản bằng OTP. Subject: "Mã xác thực QueueLink của bạn".
    /// </summary>
    public static (string subject, string html) OtpVerification(string fullName, string otpCode, int expiresMinutes)
    {
        var subject = "Mã xác thực QueueLink của bạn";
        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background: #f5f7fa; padding: 24px; color: #1f2937;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06);"">
        <div style=""background: linear-gradient(135deg, #2563eb, #1e40af); padding: 28px; text-align: center;"">
            <h1 style=""margin:0; color:#fff; font-size:24px;"">QueueLink</h1>
            <p style=""margin:6px 0 0; color:#dbeafe; font-size:13px;"">Lấy số online, tự do trải nghiệm</p>
        </div>
        <div style=""padding: 32px;"">
            <h2 style=""margin-top:0; color:#111827;"">Xin chào {(string.IsNullOrWhiteSpace(fullName) ? "bạn" : System.Net.WebUtility.HtmlEncode(fullName))},</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản QueueLink. Đây là mã OTP để xác thực email của bạn:</p>
            <div style=""text-align:center; margin: 28px 0;"">
                <div style=""display:inline-block; background:#f1f5f9; border:2px dashed #2563eb; padding:18px 32px; border-radius:10px; letter-spacing:8px; font-size:32px; font-weight:bold; color:#2563eb;"">
                    {otpCode}
                </div>
            </div>
            <p>Mã có hiệu lực trong <strong>{expiresMinutes} phút</strong>. Vui lòng không chia sẻ mã này cho ai khác.</p>
            <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>
            <hr style=""border:none; border-top:1px solid #e5e7eb; margin:24px 0;"">
            <p style=""font-size:13px; color:#6b7280; margin:0;"">Trân trọng,<br><strong>Đội ngũ QueueLink</strong></p>
        </div>
    </div>
</body>
</html>";
        return (subject, html);
    }

    /// <summary>
    /// Thông báo ""sắp đến lượt"". Gửi khi khách còn N người phía trước.
    /// </summary>
    public static (string subject, string html) UpcomingTurn(
        string fullName,
        string ticketCode,
        string venueName,
        string queueServiceName,
        int peopleAhead,
        int estimatedMinutes)
    {
        var subject = $"[QueueLink] Sắp đến lượt của bạn — {ticketCode}";
        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background: #f5f7fa; padding: 24px; color: #1f2937;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06);"">
        <div style=""background: linear-gradient(135deg, #f59e0b, #d97706); padding: 28px; text-align: center;"">
            <h1 style=""margin:0; color:#fff; font-size:24px;"">🔔 Sắp đến lượt!</h1>
            <p style=""margin:6px 0 0; color:#fef3c7; font-size:13px;"">QueueLink Notification</p>
        </div>
        <div style=""padding: 32px;"">
            <h2 style=""margin-top:0; color:#111827;"">Xin chào {(string.IsNullOrWhiteSpace(fullName) ? "bạn" : System.Net.WebUtility.HtmlEncode(fullName))},</h2>
            <p>Số thứ tự của bạn sắp được phục vụ. Vui lòng chuẩn bị tại quầy.</p>
            <div style=""background:#fef3c7; border-left:4px solid #f59e0b; padding:18px 20px; border-radius:8px; margin:24px 0;"">
                <p style=""margin:0 0 8px; color:#92400e;""><strong>Số của bạn:</strong> <span style=""font-size:22px; color:#d97706;"">{System.Net.WebUtility.HtmlEncode(ticketCode)}</span></p>
                <p style=""margin:0 0 8px; color:#92400e;""><strong>Địa điểm:</strong> {System.Net.WebUtility.HtmlEncode(venueName)} &mdash; {System.Net.WebUtility.HtmlEncode(queueServiceName)}</p>
                <p style=""margin:0; color:#92400e;""><strong>Còn khoảng:</strong> {peopleAhead} người phía trước (≈ {estimatedMinutes} phút)</p>
            </div>
            <p>Nếu bạn không quay lại trong vòng vài phút tới, nhân viên có thể chuyển sang số tiếp theo.</p>
            <hr style=""border:none; border-top:1px solid #e5e7eb; margin:24px 0;"">
            <p style=""font-size:13px; color:#6b7280; margin:0;"">Trân trọng,<br><strong>Đội ngũ QueueLink</strong></p>
        </div>
    </div>
</body>
</html>";
        return (subject, html);
    }

    /// <summary>
    /// Thông báo ""đang được gọi"". Gửi khi staff bấm CallNext và đến lượt khách.
    /// </summary>
    public static (string subject, string html) YourTurn(
        string fullName,
        string ticketCode,
        string venueName)
    {
        var subject = $"[QueueLink] Đến lượt của bạn — {ticketCode}";
        var html = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background: #f5f7fa; padding: 24px; color: #1f2937;"">
    <div style=""max-width: 560px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.06);"">
        <div style=""background: linear-gradient(135deg, #10b981, #047857); padding: 28px; text-align: center;"">
            <h1 style=""margin:0; color:#fff; font-size:24px;"">✅ Đến lượt của bạn!</h1>
            <p style=""margin:6px 0 0; color:#d1fae5; font-size:13px;"">Vui lòng quay lại quầy</p>
        </div>
        <div style=""padding: 32px;"">
            <h2 style=""margin-top:0; color:#111827;"">Xin chào {(string.IsNullOrWhiteSpace(fullName) ? "bạn" : System.Net.WebUtility.HtmlEncode(fullName))},</h2>
            <p>Số thứ tự <strong style=""color:#047857;"">{System.Net.WebUtility.HtmlEncode(ticketCode)}</strong> của bạn đang được gọi.</p>
            <p style=""font-size:18px;"">📍 Vui lòng quay lại <strong>{System.Net.WebUtility.HtmlEncode(venueName)}</strong> ngay để được phục vụ.</p>
            <hr style=""border:none; border-top:1px solid #e5e7eb; margin:24px 0;"">
            <p style=""font-size:13px; color:#6b7280; margin:0;"">Trân trọng,<br><strong>Đội ngũ QueueLink</strong></p>
        </div>
    </div>
</body>
</html>";
        return (subject, html);
    }
}
