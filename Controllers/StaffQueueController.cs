using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.Services;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Staff,Admin")]
public class StaffQueueController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IQueueTicketService _ticketService;

    public StaffQueueController(ApplicationDbContext db, IQueueTicketService ticketService)
    {
        _db = db;
        _ticketService = ticketService;
    }

    // GET: /StaffQueue
    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var queues = await _db.QueueServices
            .Include(q => q.Venue)
            .Where(q => q.IsActive)
            .Select(q => new StaffQueueListItemViewModel
            {
                QueueServiceId = q.Id,
                VenueName = q.Venue!.Name,
                QueueServiceName = q.Name,
                QueueStatus = q.QueueStatus,
                WaitingCount = q.Tickets
                    .Where(t => t.TicketDate == today
                             && t.Status == TicketStatus.Waiting)
                    .Count(),
                CurrentCallingTicketCode = q.Tickets
                    .Where(t => t.TicketDate == today && t.Status == TicketStatus.Called)
                    .OrderByDescending(t => t.CalledAt)
                    .Select(t => t.TicketCode)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return View(queues);
    }

    // GET: /StaffQueue/Details/{queueServiceId}
    public async Task<IActionResult> Details(int queueServiceId)
    {
        var today = DateTime.UtcNow.Date;
        var qs = await _db.QueueServices
            .Include(q => q.Venue)
            .FirstOrDefaultAsync(q => q.Id == queueServiceId);

        if (qs == null) return NotFound();

        var tickets = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId && t.TicketDate == today)
            .OrderBy(t => t.Status == TicketStatus.Waiting ? 0 :
                          t.Status == TicketStatus.Called ? 1 :
                          t.Status == TicketStatus.Serving ? 2 : 3)
            .ThenBy(t => t.TicketNumber)
            .Select(t => new StaffTicketRowViewModel
            {
                TicketId = t.Id,
                TicketCode = t.TicketCode,
                CustomerName = t.CustomerName,
                CustomerPhone = t.CustomerPhone,
                PartySize = t.PartySize,
                CreatedAt = t.CreatedAt,
                Status = t.Status,
                StatusText = t.Status == TicketStatus.Waiting ? "Đang chờ" :
                             t.Status == TicketStatus.Called ? "Đang gọi" :
                             t.Status == TicketStatus.Serving ? "Đang phục vụ" :
                             t.Status == TicketStatus.Completed ? "Hoàn tất" :
                             t.Status == TicketStatus.NoShow ? "Vắng mặt" : "Đã hủy",
                EstimatedWaitMinutes = t.EstimatedWaitMinutes,
                PublicToken = t.PublicToken
            })
            .ToListAsync();

        var currentCall = tickets
            .Where(t => t.Status == TicketStatus.Called)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.TicketCode)
            .FirstOrDefault();

        var vm = new StaffQueueDashboardViewModel
        {
            QueueServiceId = qs.Id,
            VenueId = qs.VenueId,
            VenueName = qs.Venue!.Name,
            QueueServiceName = qs.Name,
            Description = qs.Description,
            QueueStatus = qs.QueueStatus,
            WaitingCount = tickets.Count(t => t.Status == TicketStatus.Waiting),
            AverageEstimatedWaitMinutes = qs.AverageServiceMinutes,
            CurrentCallingTicketCode = currentCall,
            Tickets = tickets
        };

        return View(vm);
    }

    // POST: /StaffQueue/CallNext/{queueServiceId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CallNext(int queueServiceId)
    {
        var userId = User.Identity?.Name ?? "system";
        var result = await _ticketService.CallNextAsync(queueServiceId, userId);
        if (result == null)
        {
            TempData["Info"] = "Không có ticket nào đang chờ.";
        }
        return RedirectToAction(nameof(Details), new { queueServiceId });
    }

    // POST: /StaffQueue/MarkServing/{ticketId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkServing(int ticketId)
    {
        var ticket = await _db.QueueTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();
        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.Serving, User.Identity?.Name);
        return RedirectToAction(nameof(Details), new { queueServiceId = ticket.QueueServiceId });
    }

    // POST: /StaffQueue/Complete/{ticketId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int ticketId)
    {
        var ticket = await _db.QueueTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();
        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.Completed, User.Identity?.Name);
        return RedirectToAction(nameof(Details), new { queueServiceId = ticket.QueueServiceId });
    }

    // POST: /StaffQueue/NoShow/{ticketId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(int ticketId)
    {
        var ticket = await _db.QueueTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();
        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.NoShow, User.Identity?.Name);
        return RedirectToAction(nameof(Details), new { queueServiceId = ticket.QueueServiceId });
    }

    // POST: /StaffQueue/Cancel/{ticketId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int ticketId)
    {
        var ticket = await _db.QueueTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();
        await _ticketService.ChangeTicketStatusAsync(ticketId, TicketStatus.Cancelled, User.Identity?.Name);
        return RedirectToAction(nameof(Details), new { queueServiceId = ticket.QueueServiceId });
    }
}
