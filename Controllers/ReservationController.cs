using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;

namespace QueueLink.Controllers;

public class ReservationController : Controller
{
    private readonly ApplicationDbContext _db;

    public ReservationController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── /{slug}/reserve — Trang đặt trước ───────────────────────────

    [HttpGet]
    public async Task<IActionResult> Create(string slug)
    {
        var venue = await _db.Venues
            .Where(v => v.IsActive && v.Slug == slug)
            .FirstOrDefaultAsync();

        if (venue == null) return NotFound("Quán không tồn tại.");

        var today = DateTime.UtcNow.Date;

        // Chỉ hiện các bàn đang Available
        var availableTables = await _db.Tables
            .Where(t => t.VenueId == venue.Id && t.IsActive && t.Status == TableStatus.Available)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        // Các khung giờ: từ open time đến close time, cách 30 phút
        var slots = new List<TimeOnly>();
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        for (var t = venue.OpenTime; t < venue.CloseTime; t = t.AddMinutes(30))
        {
            // Chỉ hiện giờ trong tương lai (sau 30 phút)
            if (t.AddMinutes(30) >= now)
                slots.Add(t);
        }

        ViewBag.Venue = venue;
        ViewBag.AvailableTables = availableTables;
        ViewBag.TimeSlots = slots;
        // Pass venue so the @model-bound view can render @Model.Name /
        // @Model.Slug / @Model.OpenTime / @Model.CloseTime. The view
        // also reads the same fields from ViewBag.Venue, but the
        // @model directive requires a non-null instance or the
        // very first Model.Name access NREs.
        return View(venue);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string slug,
        string customerName, string customerPhone, int partySize,
        DateTime reservationDate, TimeOnly reservationTime,
        int tableId, string? notes)
    {
        var venue = await _db.Venues
            .Where(v => v.IsActive && v.Slug == slug)
            .FirstOrDefaultAsync();

        if (venue == null) return NotFound();

        var table = await _db.Tables.FindAsync(tableId);
        if (table == null || table.VenueId != venue.Id)
            return BadRequest("Bàn không hợp lệ.");

        var reservationDateTime = reservationDate.Date.Add(reservationTime.ToTimeSpan());

        // Tạo mã đặt trước
        var today = DateTime.UtcNow.Date;
        var todayCount = await _db.Reservations
            .CountAsync(r => r.VenueId == venue.Id && r.CreatedAt.Date == today) + 1;
        var resCode = $"RES-{today:yyMMdd}-{todayCount:D4}";

        var res = new Reservation
        {
            TableId = tableId,
            VenueId = venue.Id,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            PartySize = partySize,
            ReservationTime = reservationDateTime,
            ReservationCode = resCode,
            Notes = notes,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reservations.Add(res);
        await _db.SaveChangesAsync();

        ViewBag.VenueName = venue.Name;
        ViewBag.TableName = table.Name;
        ViewBag.ReservationCode = resCode;
        ViewBag.ReservationTime = reservationDateTime;
        ViewBag.HoldMinutes = res.HoldMinutes;
        ViewBag.ExpiresAt = res.ExpiresAt;

        return View("Confirmation", res);
    }

    // ── /reservation/details/{code} — Xem chi tiết đặt trước ────────

    public async Task<IActionResult> Details(string code)
    {
        var res = await _db.Reservations
            .Include(r => r.Table).ThenInclude(t => t!.Venue)
            .FirstOrDefaultAsync(r => r.ReservationCode == code);

        if (res == null) return NotFound("Không tìm thấy đặt trước.");

        return View(res);
    }
}
