using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Integrations.Email;
using QueueLink.Models;

namespace QueueLink.Services;

/// <summary>
/// Gửi email thông báo cho khách trong vòng đời ticket.
/// Được gọi từ QueueTicketService sau khi trạng thái thay đổi.
/// </summary>
public interface IQueueNotificationService
{
    /// <summary>
    /// Kiểm tra tất cả ticket Waiting, gửi email "Sắp đến lượt" cho khách còn
    /// đúng N người phía trước (N = threshold). Tránh gửi trùng bằng cờ IsNotified.
    /// </summary>
    Task NotifyUpcomingTurnsAsync(int queueServiceId, int threshold, CancellationToken ct = default);

    /// <summary>
    /// Gửi email "Đến lượt của bạn" cho khách vừa được staff gọi.
    /// </summary>
    Task NotifyYourTurnAsync(int ticketId, CancellationToken ct = default);
}

public class QueueNotificationService : IQueueNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ILogger<QueueNotificationService> _logger;

    public QueueNotificationService(ApplicationDbContext db, IEmailSender email, ILogger<QueueNotificationService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task NotifyUpcomingTurnsAsync(int queueServiceId, int threshold, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        // Tính số người phía trước cho từng ticket đang Waiting trong queue.
        var waitingTickets = await _db.QueueTickets
            .Include(t => t.QueueService!)
                .ThenInclude(q => q.Venue)
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Waiting)
            .OrderBy(t => t.TicketNumber)
            .Select(t => new
            {
                Ticket = t,
                PeopleAhead = _db.QueueTickets
                    .Count(other => other.QueueServiceId == queueServiceId
                                 && other.TicketDate == today
                                 && other.TicketNumber < t.TicketNumber
                                 && (other.Status == TicketStatus.Waiting
                                  || other.Status == TicketStatus.Called
                                  || other.Status == TicketStatus.Serving))
            })
            .ToListAsync(ct);

        foreach (var entry in waitingTickets)
        {
            if (entry.PeopleAhead != threshold) continue;

            var ticket = entry.Ticket;

            // Cần email để gửi. Nếu ticket không liên kết user (khách vãng lai), bỏ qua.
            var customer = await ResolveCustomerAsync(ticket, ct);
            if (customer == null || string.IsNullOrWhiteSpace(customer.Email)) continue;

            var (subject, html) = EmailTemplates.UpcomingTurn(
                customer.FullName,
                ticket.TicketCode,
                ticket.QueueService!.Venue?.Name ?? "",
                ticket.QueueService.Name,
                entry.PeopleAhead,
                ticket.EstimatedWaitMinutes);

            await _email.SendHtmlAsync(customer.Email, subject, html, ct);

            _logger.LogInformation("[Notify] Upcoming turn email sent to {Email} for ticket {Ticket}",
                customer.Email, ticket.TicketCode);
        }
    }

    public async Task NotifyYourTurnAsync(int ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.QueueTickets
            .Include(t => t.QueueService!)
                .ThenInclude(q => q.Venue)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null) return;

        var customer = await ResolveCustomerAsync(ticket, ct);
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email)) return;

        var (subject, html) = EmailTemplates.YourTurn(
            customer.FullName,
            ticket.TicketCode,
            ticket.QueueService?.Venue?.Name ?? "");

        await _email.SendHtmlAsync(customer.Email, subject, html, ct);

        _logger.LogInformation("[Notify] Your-turn email sent to {Email} for ticket {Ticket}",
            customer.Email, ticket.TicketCode);
    }

    /// <summary>
    /// Trả về email + họ tên khách. Ưu tiên:
    /// 1. CustomerProfile (nếu user đăng nhập và đã verify email)
    /// 2. ApplicationUser.Email + FullName (nếu có)
    /// 3. null
    /// </summary>
    private async Task<CustomerContact?> ResolveCustomerAsync(QueueTicket ticket, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ticket.UserId)) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == ticket.UserId, ct);
        if (user == null) return null;

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == ticket.UserId, ct);

        return new CustomerContact(
            user.Email ?? "",
            profile?.FullName ?? user.FullName ?? ticket.CustomerName);
    }

    private record CustomerContact(string Email, string FullName);
}
