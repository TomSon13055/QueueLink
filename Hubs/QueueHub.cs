using Microsoft.AspNetCore.SignalR;

namespace QueueLink.Hubs;

/// <summary>
/// SignalR hub used to push queue / ticket updates to connected clients.
/// Clients join one or more groups:
///   - queue_{queueServiceId}     : staff dashboard + customers viewing tickets in that queue
///   - ticket_{publicToken}       : a single customer's status page
/// </summary>
public class QueueHub : Hub
{
    public Task JoinQueueGroup(int queueServiceId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, QueueGroup(queueServiceId));
    }

    public Task LeaveQueueGroup(int queueServiceId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, QueueGroup(queueServiceId));
    }

    public Task JoinTicketGroup(string publicToken)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
        {
            return Task.CompletedTask;
        }
        return Groups.AddToGroupAsync(Context.ConnectionId, TicketGroup(publicToken));
    }

    public Task LeaveTicketGroup(string publicToken)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
        {
            return Task.CompletedTask;
        }
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TicketGroup(publicToken));
    }

    public static string QueueGroup(int queueServiceId) => $"queue_{queueServiceId}";

    public static string TicketGroup(string publicToken) => $"ticket_{publicToken}";
}