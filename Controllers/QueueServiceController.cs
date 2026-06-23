using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Admin")]
public class QueueServiceController : Controller
{
    private readonly ApplicationDbContext _db;

    public QueueServiceController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.QueueServices
            .Include(q => q.Venue)
            .Where(q => q.IsActive)
            .Select(q => new QueueServiceDetailsViewModel
            {
                Id = q.Id,
                VenueId = q.VenueId,
                VenueName = q.Venue!.Name,
                Name = q.Name,
                Description = q.Description,
                Prefix = q.Prefix,
                AverageServiceMinutes = q.AverageServiceMinutes,
                QueueStatus = q.QueueStatus,
                IsActive = q.IsActive,
                CreatedAt = q.CreatedAt,
                WaitingCount = q.Tickets
                    .Count(t => t.TicketDate == DateTime.UtcNow.Date
                             && (t.Status == TicketStatus.Waiting
                              || t.Status == TicketStatus.Called
                              || t.Status == TicketStatus.Serving)),
                PublicJoinUrl = $"/Queue/Join/{q.Id}"
            })
            .ToListAsync();

        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var qs = await _db.QueueServices
            .Include(q => q.Venue)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (qs == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var vm = new QueueServiceDetailsViewModel
        {
            Id = qs.Id,
            VenueId = qs.VenueId,
            VenueName = qs.Venue!.Name,
            Name = qs.Name,
            Description = qs.Description,
            Prefix = qs.Prefix,
            AverageServiceMinutes = qs.AverageServiceMinutes,
            QueueStatus = qs.QueueStatus,
            IsActive = qs.IsActive,
            CreatedAt = qs.CreatedAt,
            PublicJoinUrl = $"/Queue/Join/{qs.Id}",
            WaitingCount = qs.Tickets
                .Count(t => t.TicketDate == today
                         && (t.Status == TicketStatus.Waiting
                          || t.Status == TicketStatus.Called
                          || t.Status == TicketStatus.Serving))
        };

        return View(vm);
    }

    public IActionResult Create()
    {
        var vm = new QueueServiceFormViewModel();
        ViewBag.Venues = _db.Venues.Where(v => v.IsActive).ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QueueServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Venues = _db.Venues.Where(v => v.IsActive).ToList();
            return View(model);
        }

        var qs = new QueueService
        {
            VenueId = model.VenueId,
            Name = model.Name,
            Description = model.Description,
            Prefix = model.Prefix.ToUpperInvariant(),
            AverageServiceMinutes = model.AverageServiceMinutes,
            QueueStatus = model.QueueStatus,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.QueueServices.Add(qs);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Hàng chờ đã được tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var qs = await _db.QueueServices.FindAsync(id);
        if (qs == null) return NotFound();

        ViewBag.Venues = _db.Venues.Where(v => v.IsActive).ToList();
        return View(new QueueServiceFormViewModel
        {
            Id = qs.Id,
            VenueId = qs.VenueId,
            Name = qs.Name,
            Description = qs.Description,
            Prefix = qs.Prefix,
            AverageServiceMinutes = qs.AverageServiceMinutes,
            QueueStatus = qs.QueueStatus,
            IsActive = qs.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QueueServiceFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Venues = _db.Venues.Where(v => v.IsActive).ToList();
            return View(model);
        }

        var qs = await _db.QueueServices.FindAsync(id);
        if (qs == null) return NotFound();

        qs.VenueId = model.VenueId;
        qs.Name = model.Name;
        qs.Description = model.Description;
        qs.Prefix = model.Prefix.ToUpperInvariant();
        qs.AverageServiceMinutes = model.AverageServiceMinutes;
        qs.QueueStatus = model.QueueStatus;
        qs.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Hàng chờ đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ToggleStatus(int id)
    {
        var qs = await _db.QueueServices.FindAsync(id);
        if (qs == null) return NotFound();

        qs.QueueStatus = qs.QueueStatus switch
        {
            QueueStatus.Open => QueueStatus.Paused,
            QueueStatus.Paused => QueueStatus.Open,
            _ => qs.QueueStatus
        };

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Trạng thái hàng chờ đã đổi thành {qs.QueueStatus}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> QRCode(int id)
    {
        var qs = await _db.QueueServices.FindAsync(id);
        if (qs == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var joinUrl = $"{baseUrl}/Queue/Join/{id}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(qrCodeData);
        var png = qr.GetGraphic(10);

        return File(png, "image/png");
    }

    public async Task<IActionResult> DownloadQRCode(int id)
    {
        var qs = await _db.QueueServices.FindAsync(id);
        if (qs == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var joinUrl = $"{baseUrl}/Queue/Join/{id}";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(joinUrl, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(qrCodeData);
        var png = qr.GetGraphic(10);

        return File(png, "image/png", $"QueueLink_{qs.Prefix}_{id}_QR.png");
    }
}
