using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.Services;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

public class QueueController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IQueueTicketService _ticketService;
    private readonly IGuestSession _guestSession;
    private readonly UserManager<ApplicationUser> _userManager;

    public QueueController(
        ApplicationDbContext db,
        IQueueTicketService ticketService,
        IGuestSession guestSession,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _ticketService = ticketService;
        _guestSession = guestSession;
        _userManager = userManager;
    }

    // GET: /Queue/Scan
    public IActionResult Scan()
    {
        return View();
    }

    // POST: /Queue/Scan
    [HttpPost]
    public IActionResult ProcessQRCode(string qrUrl)
    {
        if (string.IsNullOrWhiteSpace(qrUrl))
        {
            TempData["Error"] = "Không nhận được mã QR. Vui lòng thử lại.";
            return RedirectToAction(nameof(Scan));
        }

        // Accept URLs like /Queue/Join/5 or /QueueService/Details/5
        // Also accept full URLs like https://example.com/Queue/Join/5
        var uri = qrUrl.Trim();

        // Try to extract the path
        try
        {
            if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var baseUri = new Uri(uri);
                uri = baseUri.AbsolutePath;
            }
        }
        catch
        {
            // Not a full URL, use as-is
        }

        // Normalize trailing slashes
        uri = uri.TrimEnd('/');

        // Match /Queue/Join/{id} → redirect to Join
        if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
            parsed.Segments.Length >= 4 &&
            parsed.Segments[1].TrimEnd('/').Equals("Queue", StringComparison.OrdinalIgnoreCase) &&
            parsed.Segments[2].TrimEnd('/').Equals("Join", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(parsed.Segments[3].TrimEnd('/'), out var queueId))
            {
                return RedirectToAction(nameof(Join), new { id = queueId });
            }
        }

        // Match /QueueService/Details/{id} or /QueueService/{id} → redirect to Details
        if (Uri.TryCreate(uri, UriKind.Absolute, out parsed) &&
            parsed.Segments.Length >= 3 &&
            parsed.Segments[1].TrimEnd('/').Equals("QueueService", StringComparison.OrdinalIgnoreCase))
        {
            var segment = parsed.Segments[2].TrimEnd('/');
            if (int.TryParse(segment, out var queueServiceId))
            {
                return RedirectToAction("Details", "QueueService", new { id = queueServiceId });
            }
            if (segment.Equals("Details", StringComparison.OrdinalIgnoreCase) && parsed.Segments.Length >= 4 &&
                int.TryParse(parsed.Segments[3].TrimEnd('/'), out queueServiceId))
            {
                return RedirectToAction("Details", "QueueService", new { id = queueServiceId });
            }
        }

        // Direct queue service ID: "5" or "/Queue/Join/5"
        if (int.TryParse(uri.TrimStart('/').Split('/').Last(), out var directId))
        {
            return RedirectToAction(nameof(Join), new { id = directId });
        }

        TempData["Error"] = "Mã QR không hợp lệ hoặc không nhận diện được. Vui lòng thử lại.";
        return RedirectToAction(nameof(Scan));
    }

    // GET: /Queue
    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var queues = await _db.QueueServices
            .Include(q => q.Venue)
            .Where(q => q.IsActive && q.Venue!.IsActive)
            .Select(q => new
            {
                q.Id,
                q.VenueId,
                VenueName = q.Venue!.Name,
                VenueSlug = q.Venue!.Slug,
                VenueLogoUrl = q.Venue!.LogoUrl,
                VenueCoverImageUrl = q.Venue!.CoverImageUrl,
                VenueAddress = q.Venue!.Address,
                q.Name,
                q.Description,
                q.QueueStatus,
                WaitingCount = q.Tickets
                    .Where(t => t.TicketDate == today
                             && (t.Status == TicketStatus.Waiting
                              || t.Status == TicketStatus.Called
                              || t.Status == TicketStatus.Serving))
                    .Count(),
                EstimatedWaitMinutes = q.Tickets
                    .Where(t => t.TicketDate == today && t.Status == TicketStatus.Waiting)
                    .Select(t => t.EstimatedWaitMinutes)
                    .ToList()
            })
            .ToListAsync();

        var result = queues.Select(q => new QueueCardViewModel
        {
            QueueServiceId = q.Id,
            VenueId = q.VenueId,
            VenueName = q.VenueName,
            VenueSlug = q.VenueSlug,
            VenueLogoUrl = q.VenueLogoUrl,
            VenueCoverImageUrl = q.VenueCoverImageUrl,
            VenueAddress = q.VenueAddress,
            QueueServiceName = q.Name,
            Description = q.Description,
            QueueStatus = q.QueueStatus,
            WaitingCount = q.WaitingCount,
            EstimatedWaitMinutes = q.EstimatedWaitMinutes.Count > 0
                ? (int)q.EstimatedWaitMinutes.Average()
                : 0
        }).ToList();

        return View(result);
    }

    // GET: /Queue/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var qs = await _db.QueueServices
            .Include(q => q.Venue)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null) return NotFound();

        return View(qs);
    }

    // GET: /Queue/Join/5
    [HttpGet]
    public async Task<IActionResult> Join(int id)
    {
        var qs = await _db.QueueServices
            .Include(q => q.Venue)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null) return NotFound();

        if (qs.QueueStatus == QueueStatus.Closed)
        {
            TempData["Error"] = "Hàng chờ này hiện đang đóng.";
            return RedirectToAction(nameof(Index));
        }

        var model = new JoinQueueViewModel
        {
            QueueServiceId = qs.Id,
            VenueName = qs.Venue!.Name,
            QueueServiceName = qs.Name,
            Prefix = qs.Prefix
        };

        // Auto-fill từ session guest profile nếu có (lần thứ 2 không cần nhập lại).
        var profile = _guestSession.Get();
        if (profile != null)
        {
            model.CustomerName = profile.FullName;
            model.CustomerPhone = profile.Phone;
        }

        return View(model);
    }

    // POST: /Queue/Join/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id, JoinQueueViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            // Nếu khách đã đăng nhập, liên kết ticket với user để gửi email thông báo.
            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userId = user?.Id;

                // Lưu profile mới nhất vào session để các lần sau auto-fill.
                _guestSession.Set(new GuestProfile
                {
                    FullName = model.CustomerName,
                    Phone = model.CustomerPhone,
                    Email = user?.Email ?? "",
                    UserId = userId
                });
            }
            else
            {
                // Khách vãng lai: lưu session để lần sau auto-fill (không có email).
                _guestSession.Set(new GuestProfile
                {
                    FullName = model.CustomerName,
                    Phone = model.CustomerPhone
                });
            }

            var ticket = await _ticketService.CreateTicketAsync(model, userId);
            return RedirectToAction(nameof(Status), new { token = ticket.PublicToken });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Queue/Status/{token}
    public async Task<IActionResult> Status(string token)
    {
        var vm = await _ticketService.GetTicketStatusAsync(token);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // GET: /Queue/GetTicketStatus/{token}  (used by SignalR JS client)
    public async Task<IActionResult> GetTicketStatus(string token)
    {
        var vm = await _ticketService.GetTicketStatusAsync(token);
        if (vm == null) return NotFound();
        return Json(vm);
    }

    // GET: /Queue/GetQueueSummary/{id}
    public async Task<IActionResult> GetQueueSummary(int id)
    {
        var summary = await _ticketService.GetQueueSummaryAsync(id);
        if (summary == null) return NotFound();
        return Json(summary);
    }
}
