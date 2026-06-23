using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.UtcNow.Date;

        var todayTickets = await _db.QueueTickets
            .Where(t => t.TicketDate == today)
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalTicketsToday = todayTickets.Count,
            WaitingTickets = todayTickets.Count(t => t.Status == TicketStatus.Waiting),
            CompletedTickets = todayTickets.Count(t => t.Status == TicketStatus.Completed),
            NoShowTickets = todayTickets.Count(t => t.Status == TicketStatus.NoShow),
            CancelledTickets = todayTickets.Count(t => t.Status == TicketStatus.Cancelled),
            AverageEstimatedWaitMinutes = todayTickets
                .Where(t => t.Status == TicketStatus.Waiting)
                .Select(t => (double?)t.EstimatedWaitMinutes)
                .DefaultIfEmpty(0)
                .Average() ?? 0,
            TotalVenues = await _db.Venues.CountAsync(v => v.IsActive),
            TotalQueueServices = await _db.QueueServices.CountAsync(q => q.IsActive),
            OpenQueues = await _db.QueueServices.CountAsync(q => q.IsActive && q.QueueStatus == QueueStatus.Open)
        };

        return View(vm);
    }
}
