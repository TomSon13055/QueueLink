using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Admin")]
public class OwnerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public OwnerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ── Chọn venue để quản lý ──────────────────────────────────────────

    public async Task<IActionResult> SelectVenue()
    {
        var venues = await _db.Venues
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new { v.Id, v.Name, v.Address })
            .ToListAsync();

        return View(venues);
    }

    // ── Dashboard chính ─────────────────────────────────────────────────

    public async Task<IActionResult> Dashboard(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Tables
        var tables = await _db.Tables
            .Where(t => t.VenueId == venueId && t.IsActive)
            .Include(t => t.Reservations.Where(r => r.Status == ReservationStatus.Confirmed
                && r.ReservationTime.Date == today && r.ReservationTime >= now))
            .Include(t => t.Orders.Where(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.Submitted))
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        // Today's reservations
        var reservations = await _db.Reservations
            .Include(r => r.Table)
            .Where(r => r.VenueId == venueId
                && r.ReservationTime.Date == today
                && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();

        // Queue summary
        var queues = await _db.QueueServices
            .Where(q => q.VenueId == venueId && q.IsActive)
            .Include(q => q.Tickets.Where(t => t.TicketDate == today
                && (t.Status == TicketStatus.Waiting || t.Status == TicketStatus.Called)))
            .ToListAsync();

        var vm = new OwnerDashboardViewModel
        {
            VenueId = venueId,
            VenueName = venue.Name,
            Tables = tables.Select(t => new TableDashboardViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Capacity = t.Capacity,
                Status = t.Status,
                SortOrder = t.SortOrder,
                IsActive = t.IsActive,
                ActiveReservation = t.Reservations.FirstOrDefault(),
                ActiveOrder = t.Orders.FirstOrDefault()
            }).ToList(),
            TodayReservations = reservations.Select(r => new ReservationListViewModel
            {
                Id = r.Id,
                ReservationCode = r.ReservationCode ?? "",
                CustomerName = r.CustomerName,
                CustomerPhone = r.CustomerPhone,
                PartySize = r.PartySize,
                ReservationTime = r.ReservationTime,
                HoldMinutes = r.HoldMinutes,
                Status = r.Status,
                TableId = r.TableId,
                TableName = r.Table?.Name ?? "",
                Notes = r.Notes
            }).ToList(),
            QueueSummary = queues.Select(q => new QueueCardViewModel
            {
                QueueServiceId = q.Id,
                VenueId = venueId,
                VenueName = venue.Name,
                VenueSlug = venue.Slug,
                VenueLogoUrl = venue.LogoUrl,
                VenueCoverImageUrl = venue.CoverImageUrl,
                VenueAddress = venue.Address,
                QueueServiceName = q.Name,
                Description = q.Description,
                QueueStatus = q.QueueStatus,
                WaitingCount = q.Tickets.Count
            }).ToList()
        };

        return View(vm);
    }

    // ════════════════════════════════════════════════════════════════════
    // TABLES
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Tables(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        var tables = await _db.Tables
            .Where(t => t.VenueId == venueId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        return View(tables);
    }

    public async Task<IActionResult> TableCreate(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        return View(new TableFormViewModel { VenueId = venueId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TableCreate(int venueId, TableFormViewModel model)
    {
        model.VenueId = venueId;
        if (!ModelState.IsValid)
        {
            ViewBag.VenueId = venueId;
            var venue = await _db.Venues.FindAsync(venueId);
            ViewBag.VenueName = venue?.Name;
            return View(model);
        }

        var table = new Table
        {
            VenueId = venueId,
            Name = model.Name,
            Capacity = model.Capacity,
            SortOrder = model.SortOrder,
            Status = TableStatus.Available,
            IsActive = model.IsActive
        };

        _db.Tables.Add(table);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm bàn.";
        return RedirectToAction(nameof(Tables), new { venueId });
    }

    public async Task<IActionResult> TableEdit(int id)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return NotFound();

        ViewBag.VenueId = table.VenueId;
        ViewBag.VenueName = (await _db.Venues.FindAsync(table.VenueId))?.Name;
        return View(new TableFormViewModel
        {
            Id = table.Id,
            VenueId = table.VenueId,
            Name = table.Name,
            Capacity = table.Capacity,
            SortOrder = table.SortOrder,
            IsActive = table.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TableEdit(int id, TableFormViewModel model)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return NotFound();

        table.Name = model.Name;
        table.Capacity = model.Capacity;
        table.SortOrder = model.SortOrder;
        table.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật bàn.";
        return RedirectToAction(nameof(Tables), new { venueId = table.VenueId });
    }

    [HttpPost]
    public async Task<IActionResult> TableDelete(int id)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return NotFound();
        var venueId = table.VenueId;
        _db.Tables.Remove(table);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa bàn.";
        return RedirectToAction(nameof(Tables), new { venueId });
    }

    // ════════════════════════════════════════════════════════════════════
    // MENU
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Menu(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        var categories = await _db.MenuCategories
            .Where(c => c.VenueId == venueId)
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        return View(categories);
    }

    public async Task<IActionResult> CategoryCreate(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        return View(new MenuCategoryViewModel { VenueId = venueId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(int venueId, MenuCategoryViewModel model)
    {
        model.VenueId = venueId;
        if (!ModelState.IsValid) return View(model);

        _db.MenuCategories.Add(new MenuCategory
        {
            VenueId = venueId,
            Name = model.Name,
            SortOrder = model.SortOrder,
            IsActive = model.IsActive
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm danh mục.";
        return RedirectToAction(nameof(Menu), new { venueId });
    }

    [HttpPost]
    public async Task<IActionResult> CategoryDelete(int id, int venueId)
    {
        var cat = await _db.MenuCategories.FindAsync(id);
        if (cat != null) { _db.MenuCategories.Remove(cat); await _db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xóa danh mục.";
        return RedirectToAction(nameof(Menu), new { venueId });
    }

    public async Task<IActionResult> MenuItemCreate(int venueId, int categoryId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        var cat = await _db.MenuCategories.FindAsync(categoryId);
        if (venue == null || cat == null) return NotFound();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        ViewBag.CategoryId = categoryId;
        ViewBag.CategoryName = cat.Name;
        return View(new MenuItemFormViewModel { VenueId = venueId, CategoryId = categoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuItemCreate(MenuItemFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.MenuItems.Add(new MenuItem
        {
            VenueId = model.VenueId,
            CategoryId = model.CategoryId,
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            ImageUrl = model.ImageUrl,
            IsActive = model.IsActive,
            IsAvailable = model.IsAvailable,
            SortOrder = model.SortOrder
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm món.";
        return RedirectToAction(nameof(Menu), new { venueId = model.VenueId });
    }

    public async Task<IActionResult> MenuItemEdit(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        var cat = await _db.MenuCategories.FindAsync(item.CategoryId);
        ViewBag.VenueId = item.VenueId;
        ViewBag.VenueName = (await _db.Venues.FindAsync(item.VenueId))?.Name;
        ViewBag.CategoryId = item.CategoryId;
        ViewBag.CategoryName = cat?.Name;

        return View(new MenuItemFormViewModel
        {
            Id = item.Id,
            VenueId = item.VenueId ?? 0,
            CategoryId = item.CategoryId,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsActive = item.IsActive,
            IsAvailable = item.IsAvailable,
            SortOrder = item.SortOrder
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MenuItemEdit(MenuItemFormViewModel model)
    {
        var item = await _db.MenuItems.FindAsync(model.Id);
        if (item == null) return NotFound();

        item.Name = model.Name;
        item.Description = model.Description;
        item.Price = model.Price;
        item.ImageUrl = model.ImageUrl;
        item.IsActive = model.IsActive;
        item.IsAvailable = model.IsAvailable;
        item.SortOrder = model.SortOrder;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật món.";
        return RedirectToAction(nameof(Menu), new { venueId = item.VenueId });
    }

    [HttpPost]
    public async Task<IActionResult> MenuItemDelete(int id, int venueId)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item != null) { _db.MenuItems.Remove(item); await _db.SaveChangesAsync(); }
        TempData["Success"] = "Đã xóa món.";
        return RedirectToAction(nameof(Menu), new { venueId });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleMenuItemAvailability(int id, int venueId)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item != null)
        {
            item.IsAvailable = !item.IsAvailable;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Menu), new { venueId });
    }

    // ════════════════════════════════════════════════════════════════════
    // STAFF ASSIGNMENT
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Staff(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        var allStaff = await _userManager.GetUsersInRoleAsync("Staff");
        var assigned = await _db.VenueStaff
            .Where(vs => vs.VenueId == venueId)
            .Select(vs => vs.UserId)
            .ToListAsync();

        var vm = allStaff.Select(u => new StaffVenueAssignmentViewModel
        {
            UserId = u.Id,
            FullName = u.FullName ?? u.UserName ?? "",
            Email = u.Email ?? "",
            IsAssigned = assigned.Contains(u.Id),
            AssignmentId = _db.VenueStaff
                .Where(vs => vs.VenueId == venueId && vs.UserId == u.Id)
                .Select(vs => (int?)vs.Id)
                .FirstOrDefault()
        }).ToList();

        ViewBag.VenueId = venueId;
        ViewBag.VenueName = venue.Name;
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> AssignStaff(int venueId, string userId)
    {
        var exists = await _db.VenueStaff
            .AnyAsync(vs => vs.VenueId == venueId && vs.UserId == userId);
        if (!exists)
        {
            _db.VenueStaff.Add(new VenueStaff { VenueId = venueId, UserId = userId });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Staff), new { venueId });
    }

    [HttpPost]
    public async Task<IActionResult> UnassignStaff(int assignmentId, int venueId)
    {
        var vs = await _db.VenueStaff.FindAsync(assignmentId);
        if (vs != null) { _db.VenueStaff.Remove(vs); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Staff), new { venueId });
    }

    // ════════════════════════════════════════════════════════════════════
    // VENUE SETTINGS
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> VenueSettings(int venueId)
    {
        var venue = await _db.Venues.FindAsync(venueId);
        if (venue == null) return NotFound();

        return View(new VenueFormViewModel
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = venue.Address,
            Phone = venue.Phone,
            LogoUrl = venue.LogoUrl,
            CoverImageUrl = venue.CoverImageUrl,
            Slug = venue.Slug,
            OpenTime = venue.OpenTime,
            CloseTime = venue.CloseTime,
            IsActive = venue.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VenueSettings(VenueFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var venue = await _db.Venues.FindAsync(model.Id);
        if (venue == null) return NotFound();

        venue.Name = model.Name;
        venue.Description = model.Description;
        venue.Address = model.Address;
        venue.Phone = model.Phone;
        venue.LogoUrl = model.LogoUrl;
        venue.CoverImageUrl = model.CoverImageUrl;
        venue.Slug = model.Slug;
        venue.OpenTime = model.OpenTime;
        venue.CloseTime = model.CloseTime;
        venue.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu cài đặt quán.";
        return RedirectToAction(nameof(Dashboard), new { venueId = venue.Id });
    }
}
