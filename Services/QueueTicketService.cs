using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Hubs;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Services;

public class QueueTicketService : IQueueTicketService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<QueueHub> _hub;

    public QueueTicketService(ApplicationDbContext db, IHubContext<QueueHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<QueueTicket> CreateTicketAsync(JoinQueueViewModel model, string? userId = null, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var qs = await _db.QueueServices
            .Include(q => q.Venue)
            .FirstOrDefaultAsync(q => q.Id == model.QueueServiceId, ct)
            ?? throw new InvalidOperationException("Hàng chờ không tồn tại.");

        if (qs.QueueStatus != QueueStatus.Open)
            throw new InvalidOperationException("Hàng chờ hiện không mở.");

        var ticket = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var lastNumber = await _db.QueueTickets
                .Where(t => t.QueueServiceId == model.QueueServiceId && t.TicketDate == today)
                .OrderByDescending(t => t.TicketNumber)
                .Select(t => (int?)t.TicketNumber)
                .FirstOrDefaultAsync(ct);

            var ticketNumber = (lastNumber ?? 0) + 1;

            var newTicket = new QueueTicket
            {
                QueueServiceId = model.QueueServiceId,
                TicketNumber = ticketNumber,
                TicketCode = $"{qs.Prefix}{ticketNumber:D3}",
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                PartySize = model.PartySize,
                Status = TicketStatus.Waiting,
                TicketDate = today,
                CreatedAt = DateTime.UtcNow,
                EstimatedWaitMinutes = 0,
                PublicToken = Guid.NewGuid().ToString("N"),
                UserId = userId
            };

            _db.QueueTickets.Add(newTicket);
            await _db.SaveChangesAsync(ct);

            var peopleAhead = await GetPeopleAheadInternalAsync(newTicket.Id, ct);
            newTicket.EstimatedWaitMinutes = peopleAhead * qs.AverageServiceMinutes;
            await _db.SaveChangesAsync(ct);

            _db.TicketStatusHistories.Add(new TicketStatusHistory
            {
                QueueTicketId = newTicket.Id,
                OldStatus = null,
                NewStatus = TicketStatus.Waiting.ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            await ticket.CommitAsync(ct);

            await _hub.Clients.Group(QueueHub.QueueGroup(model.QueueServiceId))
                .SendAsync("QueueUpdated", new { queueServiceId = model.QueueServiceId }, ct);

            return newTicket;
        }
        catch
        {
            await ticket.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<QueueTicket?> CallNextAsync(int queueServiceId, string userId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var next = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Waiting)
            .OrderBy(t => t.TicketNumber)
            .FirstOrDefaultAsync(ct);

        if (next == null) return null;

        await ChangeTicketStatusAsync(next.Id, TicketStatus.Called, userId, "Staff called next", ct);
        await RecalculateEtasAsync(queueServiceId, ct);

        await _hub.Clients.Group(QueueHub.QueueGroup(queueServiceId))
            .SendAsync("CurrentlyCallingChanged", new
            {
                queueServiceId,
                ticketCode = next.TicketCode,
                ticketId = next.Id,
                publicToken = next.PublicToken
            }, ct);

        return next;
    }

    public async Task<bool> ChangeTicketStatusAsync(int ticketId, TicketStatus newStatus, string? userId, string? note = null, CancellationToken ct = default)
    {
        var ticket = await _db.QueueTickets
            .Include(t => t.QueueService)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket == null) return false;

        var oldStatus = ticket.Status;
        ticket.Status = newStatus;

        switch (newStatus)
        {
            case TicketStatus.Called: ticket.CalledAt = DateTime.UtcNow; break;
            case TicketStatus.Serving: ticket.ServedAt = DateTime.UtcNow; break;
            case TicketStatus.Completed: ticket.CompletedAt = DateTime.UtcNow; break;
            case TicketStatus.Cancelled: ticket.CancelledAt = DateTime.UtcNow; break;
        }

        _db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            QueueTicketId = ticketId,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            Note = note,
            ChangedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group(QueueHub.QueueGroup(ticket.QueueServiceId))
            .SendAsync("TicketUpdated", new
            {
                ticketId = ticket.Id,
                ticketCode = ticket.TicketCode,
                status = newStatus.ToString(),
                publicToken = ticket.PublicToken,
                queueServiceId = ticket.QueueServiceId
            }, ct);

        await _hub.Clients.Group(QueueHub.TicketGroup(ticket.PublicToken))
            .SendAsync("TicketUpdated", new
            {
                ticketId = ticket.Id,
                ticketCode = ticket.TicketCode,
                status = newStatus.ToString(),
                publicToken = ticket.PublicToken,
                queueServiceId = ticket.QueueServiceId
            }, ct);

        await RecalculateEtasAsync(ticket.QueueServiceId, ct);

        return true;
    }

    public async Task<TicketStatusViewModel?> GetTicketStatusAsync(string publicToken, CancellationToken ct = default)
    {
        var ticket = await _db.QueueTickets
            .Include(t => t.QueueService!)
                .ThenInclude(q => q.Venue)
            .Include(t => t.QueueService)
            .FirstOrDefaultAsync(t => t.PublicToken == publicToken, ct);

        if (ticket == null) return null;

        var peopleAhead = await GetPeopleAheadAsync(ticket.Id, ct);

        var today = DateTime.UtcNow.Date;
        var currentCall = await _db.QueueTickets
            .Where(t => t.QueueServiceId == ticket.QueueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Called)
            .OrderByDescending(t => t.CalledAt)
            .Select(t => t.TicketCode)
            .FirstOrDefaultAsync(ct);

        return new TicketStatusViewModel
        {
            TicketId = ticket.Id,
            TicketCode = ticket.TicketCode,
            CustomerName = ticket.CustomerName,
            CustomerPhone = ticket.CustomerPhone,
            PartySize = ticket.PartySize,
            VenueName = ticket.QueueService?.Venue?.Name ?? "",
            QueueServiceName = ticket.QueueService?.Name ?? "",
            Status = ticket.Status,
            StatusText = GetStatusText(ticket.Status),
            PeopleAhead = peopleAhead,
            EstimatedWaitMinutes = ticket.EstimatedWaitMinutes,
            CurrentCallingTicketCode = currentCall,
            Message = GetStatusMessage(ticket.Status),
            PublicToken = ticket.PublicToken,
            QueueServiceId = ticket.QueueServiceId,
            CreatedAt = ticket.CreatedAt
        };
    }

    public async Task RecalculateEtasAsync(int queueServiceId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var avgMinutes = await _db.QueueServices
            .Where(q => q.Id == queueServiceId)
            .Select(q => q.AverageServiceMinutes)
            .FirstOrDefaultAsync(ct);

        var waitingTickets = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Waiting)
            .OrderBy(t => t.TicketNumber)
            .ToListAsync(ct);

        for (var i = 0; i < waitingTickets.Count; i++)
        {
            waitingTickets[i].EstimatedWaitMinutes = i * avgMinutes;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetPeopleAheadAsync(int ticketId, CancellationToken ct = default)
    {
        return await GetPeopleAheadInternalAsync(ticketId, ct);
    }

    public async Task<QueueSummaryDto?> GetQueueSummaryAsync(int queueServiceId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var qs = await _db.QueueServices.FindAsync(new object[] { queueServiceId }, ct);
        if (qs == null) return null;

        var waitingCount = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called || t.Status == TicketStatus.Serving))
            .CountAsync(ct);

        var currentCall = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Called)
            .OrderByDescending(t => t.CalledAt)
            .Select(t => (string?)t.TicketCode)
            .FirstOrDefaultAsync(ct);

        var avgWaiting = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId
                     && t.TicketDate == today
                     && t.Status == TicketStatus.Waiting)
            .Select(t => (double?)t.EstimatedWaitMinutes)
            .AverageAsync(ct) ?? 0;

        return new QueueSummaryDto
        {
            QueueServiceId = queueServiceId,
            WaitingCount = waitingCount,
            AverageEstimatedWaitMinutes = (int)avgWaiting,
            CurrentCallingTicketCode = currentCall,
            QueueStatus = qs.QueueStatus
        };
    }

    private async Task<int> GetPeopleAheadInternalAsync(int ticketId, CancellationToken ct)
    {
        var ticket = await _db.QueueTickets
            .Where(t => t.Id == ticketId)
            .Select(t => new { t.QueueServiceId, t.TicketNumber, t.TicketDate })
            .FirstOrDefaultAsync(ct);

        if (ticket == null) return 0;

        return await _db.QueueTickets
            .Where(t => t.QueueServiceId == ticket.QueueServiceId
                     && t.TicketDate == ticket.TicketDate
                     && t.TicketNumber < ticket.TicketNumber
                     && (t.Status == TicketStatus.Waiting
                      || t.Status == TicketStatus.Called
                      || t.Status == TicketStatus.Serving))
            .CountAsync(ct);
    }

    private static string GetStatusText(TicketStatus status) => status switch
    {
        TicketStatus.Waiting => "Đang chờ",
        TicketStatus.Called => "Đang gọi",
        TicketStatus.Serving => "Đang phục vụ",
        TicketStatus.Completed => "Hoàn tất",
        TicketStatus.NoShow => "Vắng mặt",
        TicketStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    private static string GetStatusMessage(TicketStatus status) => status switch
    {
        TicketStatus.Waiting => "Bạn đang trong hàng chờ. Có thể di chuyển xung quanh, vui lòng theo dõi màn hình này.",
        TicketStatus.Called => "Đang gọi số của bạn. Vui lòng quay lại quầy.",
        TicketStatus.Serving => "Bạn đang được phục vụ.",
        TicketStatus.Completed => "Lượt của bạn đã hoàn tất. Cảm ơn bạn!",
        TicketStatus.NoShow => "Bạn đã bị đánh dấu vắng mặt.",
        TicketStatus.Cancelled => "Lượt đã bị hủy.",
        _ => ""
    };
}
