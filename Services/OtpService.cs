using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Integrations.Email;
using QueueLink.Models;

namespace QueueLink.Services;

/// <summary>
/// Quản lý vòng đời OTP: sinh mã, gửi email, xác thực.
/// </summary>
public interface IOtpService
{
    Task<string> GenerateAndSendAsync(string email, string fullName, CancellationToken ct = default);
    Task<bool> VerifyAsync(string email, string code, CancellationToken ct = default);
    Task ResendAsync(string email, string fullName, CancellationToken ct = default);
    Task<int> PurgeExpiredAsync(CancellationToken ct = default);
}

public class OtpService : IOtpService
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private const int OtpLength = 6;
    private const int MaxResendsPerDay = 5;

    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<OtpService> _logger;

    public OtpService(ApplicationDbContext db, IEmailSender email, ILogger<OtpService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendAsync(string email, string fullName, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, OtpLength))
            .ToString($"D{OtpLength}");

        var (subject, html) = EmailTemplates.OtpVerification(fullName, code, (int)OtpLifetime.TotalMinutes);
        var sent = await _email.SendHtmlAsync(normalized, subject, html, ct);

        if (!sent)
            throw new InvalidOperationException($"Không thể gửi email OTP tới {normalized}.");

        var otp = new EmailOtp
        {
            Email = normalized,
            OtpHash = Hash(code),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(OtpLifetime),
            IsUsed = false,
            AttemptCount = 0
        };

        _db.EmailOtps.Add(otp);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[Otp] Issued OTP for {Email}", normalized);
        return code;
    }

    public async Task<bool> VerifyAsync(string email, string code, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var otp = await _db.EmailOtps
            .Where(o => o.Email == normalized && !o.IsUsed && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp == null) return false;

        // Rate-limit: tối đa 5 lần thử sai cho mỗi OTP.
        if (otp.AttemptCount >= 5)
        {
            _logger.LogWarning("[Otp] Too many attempts for {Email}", normalized);
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(otp.OtpHash),
                Encoding.UTF8.GetBytes(Hash(code))))
        {
            otp.AttemptCount++;
            await _db.SaveChangesAsync(ct);
            return false;
        }

        otp.IsUsed = true;
        otp.UsedAt = now;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ResendAsync(string email, string fullName, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var todayStart = DateTime.UtcNow.Date;

        var sentToday = await _db.EmailOtps
            .Where(o => o.Email == normalized && o.CreatedAt >= todayStart)
            .CountAsync(ct);

        if (sentToday >= MaxResendsPerDay)
            throw new InvalidOperationException("Bạn đã yêu cầu gửi lại quá nhiều lần. Vui lòng thử lại sau.");

        await GenerateAndSendAsync(email, fullName, ct);
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        var deleted = await _db.EmailOtps
            .Where(o => o.ExpiresAt < cutoff && (o.IsUsed || o.ExpiresAt < DateTime.UtcNow))
            .ExecuteDeleteAsync(ct);
        return deleted;
    }

    private static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}
