using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.ViewModels;
using QueueStatus = QueueLink.Models.QueueStatus;
using TicketStatus = QueueLink.Models.TicketStatus;
using TableStatus = QueueLink.Models.TableStatus;

namespace QueueLink.Controllers;

// ── View Models ────────────────────────────────────────────────────────────

public class PublicVenueViewModel
{
    public Venue Venue { get; set; } = null!;
    public List<PublicTableViewModel> Tables { get; set; } = new();
    public bool HasAvailableTables { get; set; }
    public List<PublicQueueViewModel> Queues { get; set; } = new();
}

public class PublicVenueCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public int AvailableTableCount { get; set; }
    public int TotalTableCount { get; set; }
    public int OpenQueueCount { get; set; }
}

public class PublicTableViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public TableStatus Status { get; set; }
    public string? ReservationCode { get; set; }
    public string? ReservationCustomer { get; set; }
    public DateTime? ReservationTime { get; set; }
    public decimal LayoutX { get; set; }
    public decimal LayoutY { get; set; }
    public decimal LayoutW { get; set; }
    public decimal LayoutH { get; set; }
    public string? Block { get; set; }
}

public class PublicQueueViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int WaitingCount { get; set; }
}

// ── Controller ────────────────────────────────────────────────────────────

public class PublicController : Controller
{
    private readonly ApplicationDbContext _db;

    public PublicController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Venues — Browse all active venues
    public async Task<IActionResult> Browse()
    {
        var today = DateTime.UtcNow.Date;

        var venues = await _db.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new PublicVenueCardViewModel
            {
                Id = v.Id,
                Name = v.Name,
                Slug = v.Slug ?? "",
                Address = v.Address,
                LogoUrl = v.LogoUrl,
                CoverImageUrl = v.CoverImageUrl,
                Description = v.Description,
                OpenTime = v.OpenTime,
                CloseTime = v.CloseTime,
                AvailableTableCount = _db.Tables.Count(t => t.VenueId == v.Id && t.IsActive && t.Status == TableStatus.Available),
                TotalTableCount = _db.Tables.Count(t => t.VenueId == v.Id && t.IsActive),
                OpenQueueCount = _db.QueueServices.Count(q => q.VenueId == v.Id && q.IsActive && q.QueueStatus == QueueStatus.Open)
            })
            .ToListAsync();

        return View(venues);
    }

    public async Task<IActionResult> Index(string slug)
    {
        var venue = await _db.Venues
            .Where(v => v.IsActive && v.Slug == slug)
            .FirstOrDefaultAsync();

        if (venue == null) return NotFound("Quán không tồn tại.");

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var tables = await _db.Tables
            .Where(t => t.VenueId == venue.Id && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        var reservations = await _db.Reservations
            .Where(r => r.VenueId == venue.Id
                && r.ReservationTime.Date == today
                && r.Status == ReservationStatus.Confirmed
                && r.ReservationTime.AddMinutes(r.HoldMinutes) >= now)
            .ToListAsync();

        // Layout* / Block are [NotMapped] on Table — fetch via raw SQL.
        var layoutRows = await _db.Database
            .SqlQueryRaw<TableLayoutRow>(
                @"SELECT ""Id"", ""LayoutX"", ""LayoutY"", ""LayoutW"", ""LayoutH"", ""Block""
                  FROM ""Tables""
                  WHERE ""VenueId"" = {0} AND ""IsActive""", venue.Id)
            .ToListAsync();

        var layoutMap = layoutRows.ToDictionary(r => r.Id);

        bool hasAvailable = tables.Any(t => t.Status == TableStatus.Available);

        var queues = await _db.QueueServices
            .Where(q => q.VenueId == venue.Id && q.IsActive && q.QueueStatus == QueueStatus.Open)
            .Include(q => q.Tickets.Where(t => t.TicketDate == today
                && (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called)))
            .ToListAsync();

        var vm = new PublicVenueViewModel
        {
            Venue = venue,
            Tables = tables.Select(t =>
            {
                var res = reservations.FirstOrDefault(r => r.TableId == t.Id);
                layoutMap.TryGetValue(t.Id, out var layout);
                return new PublicTableViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Capacity = t.Capacity,
                    Status = t.Status,
                    ReservationCode = res?.ReservationCode,
                    ReservationCustomer = res?.CustomerName,
                    ReservationTime = res?.ReservationTime,
                    LayoutX = layout?.LayoutX ?? 50m,
                    LayoutY = layout?.LayoutY ?? 50m,
                    LayoutW = layout?.LayoutW ?? 12m,
                    LayoutH = layout?.LayoutH ?? 9m,
                    Block = layout?.Block
                };
            }).ToList(),
            HasAvailableTables = hasAvailable,
            Queues = queues.Select(q => new PublicQueueViewModel
            {
                Id = q.Id,
                Name = q.Name,
                WaitingCount = q.Tickets.Count
            }).ToList()
        };

        ViewData["Title"] = venue.Name;
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Queue(string slug)
    {
        var venue = await _db.Venues
            .Where(v => v.IsActive && v.Slug == slug)
            .FirstOrDefaultAsync();

        if (venue == null) return NotFound();

        var hasAvailable = await _db.Tables
            .AnyAsync(t => t.VenueId == venue.Id && t.IsActive && t.Status == TableStatus.Available);

        if (hasAvailable)
            return RedirectToAction(nameof(Index), new { slug });

        var queues = await _db.QueueServices
            .Where(q => q.VenueId == venue.Id && q.IsActive && q.QueueStatus == QueueStatus.Open)
            .ToListAsync();

        if (!queues.Any()) return NotFound("Không có hàng đợi nào đang mở.");

        ViewBag.Venue = venue;
        ViewBag.Queues = queues;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Queue(string slug, string customerName, string customerPhone, int queueServiceId)
    {
        var venue = await _db.Venues
            .Where(v => v.IsActive && v.Slug == slug)
            .FirstOrDefaultAsync();
        if (venue == null) return NotFound();

        var queue = await _db.QueueServices.FindAsync(queueServiceId);
        if (queue == null || queue.VenueId != venue.Id) return NotFound();

        var today = DateTime.UtcNow.Date;
        var lastTicket = await _db.QueueTickets
            .Where(t => t.QueueServiceId == queueServiceId && t.TicketDate == today)
            .OrderByDescending(t => t.TicketNumber)
            .FirstOrDefaultAsync();

        var nextNumber = (lastTicket?.TicketNumber ?? 0) + 1;
        var ticketCode = $"{queue.Prefix}{nextNumber:D3}";

        var ticket = new QueueTicket
        {
            QueueServiceId = queueServiceId,
            TicketNumber = nextNumber,
            TicketCode = ticketCode,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            PartySize = 1,
            Status = TicketStatus.Waiting,
            TicketDate = today,
            CreatedAt = DateTime.UtcNow,
            PublicToken = Guid.NewGuid().ToString("N"),
            EstimatedWaitMinutes = nextNumber * queue.AverageServiceMinutes
        };

        _db.QueueTickets.Add(ticket);
        await _db.SaveChangesAsync();

        var waitingCount = await _db.QueueTickets
            .CountAsync(t => t.QueueServiceId == queueServiceId && t.TicketDate == today
                && t.Status == TicketStatus.Waiting);

        ViewBag.Ticket = ticket;
        ViewBag.WaitingAhead = waitingCount - 1;
        ViewBag.VenueName = venue.Name;
        return View("QueueSuccess");
    }

    public async Task<IActionResult> TicketStatusView(string token)
    {
        var ticket = await _db.QueueTickets
            .Include(t => t.QueueService).ThenInclude(q => q!.Venue)
            .FirstOrDefaultAsync(t => t.PublicToken == token);

        if (ticket == null) return NotFound("Không tìm thấy số của bạn.");

        var today = DateTime.UtcNow.Date;
        var waitingAhead = await _db.QueueTickets
            .CountAsync(t => t.QueueServiceId == ticket.QueueServiceId
                && t.TicketDate == today
                && t.Status == TicketStatus.Waiting
                && t.TicketNumber < ticket.TicketNumber);

        var currentlyServing = await _db.QueueTickets
            .Where(t => t.QueueServiceId == ticket.QueueServiceId
                && t.TicketDate == today
                && t.Status == TicketStatus.Serving)
            .FirstOrDefaultAsync();

        ViewBag.WaitingAhead = waitingAhead;
        ViewBag.CurrentlyServing = currentlyServing;
        return View(ticket);
    }
}
