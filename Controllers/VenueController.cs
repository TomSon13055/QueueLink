using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Admin")]
public class VenueController : Controller
{
    private readonly ApplicationDbContext _db;

    public VenueController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var venues = await _db.Venues
            .Select(v => new VenueFormViewModel
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Address = v.Address,
                Phone = v.Phone,
                LogoUrl = v.LogoUrl,
                IsActive = v.IsActive
            })
            .ToListAsync();

        return View(venues);
    }

    public async Task<IActionResult> Details(int id)
    {
        var v = await _db.Venues
            .Include(venue => venue.QueueServices)
            .FirstOrDefaultAsync(venue => venue.Id == id);

        if (v == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var vm = new VenueDetailsViewModel
        {
            Id = v.Id,
            Name = v.Name,
            Description = v.Description,
            Address = v.Address,
            Phone = v.Phone,
            LogoUrl = v.LogoUrl,
            IsActive = v.IsActive,
            CreatedAt = v.CreatedAt,
            QueueServices = v.QueueServices.Select(qs => new QueueCardViewModel
            {
                QueueServiceId = qs.Id,
                VenueId = v.Id,
                VenueName = v.Name,
                QueueServiceName = qs.Name,
                Description = qs.Description,
                QueueStatus = qs.QueueStatus,
                WaitingCount = qs.Tickets
                    .Count(t => t.TicketDate == today
                             && (t.Status == Models.TicketStatus.Waiting
                              || t.Status == Models.TicketStatus.Called
                              || t.Status == Models.TicketStatus.Serving)),
                EstimatedWaitMinutes = (int)(qs.Tickets
                    .Where(t => t.TicketDate == today && t.Status == Models.TicketStatus.Waiting)
                    .Select(t => (int?)t.EstimatedWaitMinutes)
                    .DefaultIfEmpty(0)
                    .Average() ?? 0)
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Create() => View(new VenueFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VenueFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var venue = new Models.Venue
        {
            Name = model.Name,
            Description = model.Description,
            Address = model.Address,
            Phone = model.Phone,
            LogoUrl = model.LogoUrl,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Địa điểm đã được tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var v = await _db.Venues.FindAsync(id);
        if (v == null) return NotFound();

        return View(new VenueFormViewModel
        {
            Id = v.Id,
            Name = v.Name,
            Description = v.Description,
            Address = v.Address,
            Phone = v.Phone,
            LogoUrl = v.LogoUrl,
            IsActive = v.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VenueFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var v = await _db.Venues.FindAsync(id);
        if (v == null) return NotFound();

        v.Name = model.Name;
        v.Description = model.Description;
        v.Address = model.Address;
        v.Phone = model.Phone;
        v.LogoUrl = model.LogoUrl;
        v.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Địa điểm đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Deactivate(int id)
    {
        var v = await _db.Venues.FindAsync(id);
        if (v == null) return NotFound();
        v.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Địa điểm đã được vô hiệu hóa.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Activate(int id)
    {
        var v = await _db.Venues.FindAsync(id);
        if (v == null) return NotFound();
        v.IsActive = true;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Địa điểm đã được kích hoạt.";
        return RedirectToAction(nameof(Index));
    }
}
