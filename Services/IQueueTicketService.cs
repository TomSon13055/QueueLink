using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Services;

public interface IQueueTicketService
{
    Task<QueueTicket> CreateTicketAsync(JoinQueueViewModel model, string? userId = null, CancellationToken ct = default);

    Task<QueueTicket?> CallNextAsync(int queueServiceId, string userId, CancellationToken ct = default);

    Task<bool> ChangeTicketStatusAsync(int ticketId, TicketStatus newStatus, string? userId, string? note = null, CancellationToken ct = default);

    Task<TicketStatusViewModel?> GetTicketStatusAsync(string publicToken, CancellationToken ct = default);

    Task RecalculateEtasAsync(int queueServiceId, CancellationToken ct = default);

    Task<int> GetPeopleAheadAsync(int ticketId, CancellationToken ct = default);

    Task<QueueSummaryDto?> GetQueueSummaryAsync(int queueServiceId, CancellationToken ct = default);
}