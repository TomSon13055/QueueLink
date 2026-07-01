using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

[Authorize(Roles = "Staff,Admin")]
public class StaffController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<int?> GetAssignedVenueId()
    {
        if (User.IsInRole("Admin")) return null; // Admin thấy tất cả
        var userId = _userManager.GetUserId(User);
        var assignment = await _db.VenueStaff.FirstOrDefaultAsync(vs => vs.UserId == userId);
        return assignment?.VenueId;
    }

    // ════════════════════════════════════════════════════════════════════
    // FLOOR PLAN (Sơ đồ bàn) — GET
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> FloorPlan(int? venueId)
    {
        int? vid = venueId ?? await GetAssignedVenueId();
        if (vid == null) return RedirectToAction("SelectVenue", "Owner");

        var venue = await _db.Venues.FindAsync(vid.Value);
        if (venue == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Load tables — Block and Layout* are [NotMapped] so EF never
        // SELECTs those columns.  We fetch them from raw SQL below.
        var tables = await _db.Tables
            .Where(t => t.VenueId == vid && t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        // Read layout data from the actual columns via raw SQL.
        // Layout* / Block are managed exclusively through raw SQL.
        var layoutRows = await _db.Database
            .SqlQueryRaw<TableLayoutRow>(
                @"SELECT ""Id"", ""LayoutX"", ""LayoutY"", ""LayoutW"", ""LayoutH"", ""Block""
                  FROM ""Tables""
                  WHERE ""VenueId"" = {0} AND ""IsActive""", vid.Value)
            .ToListAsync();

        var layoutMap = layoutRows.ToDictionary(r => r.Id);

        // Reservations and Orders are loaded separately, then
        // filtered by computed ExpiresAt in memory. We can't ask EF
        // to filter by ExpiresAt because it's a computed property
        // (ReservationTime + HoldMinutes) with no SQL column.
        var reservations = await _db.Reservations
            .Where(r => r.Table!.VenueId == vid
                     && r.Status == ReservationStatus.Confirmed
                     && r.ReservationTime.Date == today
                     && r.ReservationTime.AddMinutes(r.HoldMinutes) >= now)
            .ToListAsync();

        var orders = await _db.Orders
            .Where(o => o.Table!.VenueId == vid
                     && (o.Status == OrderStatus.Open
                      || o.Status == OrderStatus.Submitted
                      || o.Status == OrderStatus.Served))
            .ToListAsync();

        var resByTable = reservations
            .GroupBy(r => r.TableId)
            .ToDictionary(g => g.Key, g => g.First());

        var orderByTable = orders
            .GroupBy(o => o.TableId)
            .ToDictionary(g => g.Key, g => g.First());

        var vms = tables.Select(t =>
        {
            layoutMap.TryGetValue(t.Id, out var layout);
            return new TableDashboardViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Capacity = t.Capacity,
                Status = t.Status,
                SortOrder = t.SortOrder,
                IsActive = t.IsActive,
                LayoutX = layout?.LayoutX ?? 50m,
                LayoutY = layout?.LayoutY ?? 50m,
                LayoutW = layout?.LayoutW ?? 12m,
                LayoutH = layout?.LayoutH ?? 9m,
                Block = layout?.Block,
                ActiveReservation = resByTable.TryGetValue(t.Id, out var r) ? r : null,
                ActiveOrder = orderByTable.TryGetValue(t.Id, out var o) ? o : null,
            };
        }).ToList();

        ViewBag.VenueId = vid.Value;
        ViewBag.VenueName = venue.Name;
        ViewBag.IsAdmin = User.IsInRole("Admin");
        return View(vms);
    }

    // ════════════════════════════════════════════════════════════════════
    // SEAT TABLE (Xếp khách vào bàn)
    // ════════════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SeatTable(int tableId, int? reservationId)
    {
        var table = await _db.Tables
            .Include(t => t.Venue)
            .FirstOrDefaultAsync(t => t.Id == tableId);
        if (table == null) return NotFound();

        Reservation? reservation = null;
        if (reservationId.HasValue)
            reservation = await _db.Reservations.FindAsync(reservationId.Value);

        var vm = new SeatTableViewModel
        {
            TableId = table.Id,
            TableName = table.Name,
            VenueId = table.VenueId,
            VenueName = table.Venue?.Name ?? "",
            ReservationId = reservation?.Id,
            ReservationCode = reservation?.ReservationCode,
            CustomerName = reservation?.CustomerName ?? "",
            CustomerPhone = reservation?.CustomerPhone ?? "",
            PartySize = reservation?.PartySize ?? 1,
            Notes = reservation?.Notes
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeatTable(SeatTableViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var table = await _db.Tables.FindAsync(model.TableId);
        if (table == null) return NotFound();

        // Mark reservation as seated
        if (model.ReservationId.HasValue)
        {
            var res = await _db.Reservations.FindAsync(model.ReservationId.Value);
            if (res != null)
            {
                res.Status = ReservationStatus.Seated;
                res.SeatedAt = DateTime.UtcNow;
            }
        }

        // Create new order for the table
        var userId = _userManager.GetUserId(User);
        var order = new Order
        {
            TableId = table.Id,
            VenueId = model.VenueId,
            PartySize = model.PartySize,
            CustomerName = model.CustomerName,
            Status = OrderStatus.Open,
            StaffId = userId,
            ReservationId = model.ReservationId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);

        // Update table status
        table.Status = TableStatus.Occupied;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã xếp khách vào {table.Name}.";
        return RedirectToAction(nameof(OrderItems), new { orderId = order.Id });
    }

    // ════════════════════════════════════════════════════════════════════
    // ORDER ITEMS (Gọi món) — GET + SEARCH
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> OrderItems(int orderId, string? search)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        var categories = await _db.MenuCategories
            .Where(c => c.VenueId == order.VenueId && c.IsActive)
            .Include(c => c.Items.Where(i => i.IsActive))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        // Sub-total calculation
        order.SubTotal = order.Items.Sum(i => i.TotalPrice);
        order.TaxAmount = 0; // chưa có tax
        order.TotalAmount = order.SubTotal + order.TaxAmount - order.DiscountAmount;
        await _db.SaveChangesAsync();

        ViewBag.Order = order;
        ViewBag.Search = search ?? "";
        return View(categories);
    }

    public async Task<IActionResult> SearchMenuItems(int orderId, string? q)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        var items = await _db.MenuItems
            .Include(m => m.Category)
            .Where(m => m.VenueId == order.VenueId && m.IsActive && m.IsAvailable)
            .Where(m => string.IsNullOrEmpty(q) ||
                m.Name.ToLower().Contains(q!.ToLower()) ||
                (m.Description != null && m.Description.ToLower().Contains(q!.ToLower())))
            .OrderBy(m => m.Name)
            .Take(20)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Description,
                m.Price,
                CategoryName = m.Category != null ? m.Category.Name : "",
                IsAlreadyAdded = _db.OrderItems.Any(oi => oi.OrderId == orderId && oi.MenuItemId == m.Id)
            })
            .ToListAsync();

        return Json(items);
    }

    [HttpPost]
    public async Task<IActionResult> AddOrderItem(int orderId, int menuItemId, int quantity, string? notes)
    {
        if (quantity < 1) quantity = 1;

        var menuItem = await _db.MenuItems.FindAsync(menuItemId);
        if (menuItem == null) return NotFound();

        // Check if already in order
        var existing = await _db.OrderItems
            .FirstOrDefaultAsync(oi => oi.OrderId == orderId && oi.MenuItemId == menuItemId);

        if (existing != null)
        {
            existing.Quantity += quantity;
            existing.Notes = string.IsNullOrEmpty(notes) ? existing.Notes : notes;
        }
        else
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                ItemName = menuItem.Name,
                Quantity = quantity,
                UnitPrice = menuItem.Price,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        var order = await _db.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.SubTotal = (await _db.OrderItems.Where(oi => oi.OrderId == orderId).ToListAsync()).Sum(i => i.TotalPrice);
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderItemQuantity(int itemId, int quantity)
    {
        var item = await _db.OrderItems.FindAsync(itemId);
        if (item == null) return NotFound();

        if (quantity <= 0)
        {
            _db.OrderItems.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
            var order = await _db.Orders.FindAsync(item.OrderId);
            if (order != null)
            {
                order.SubTotal = (await _db.OrderItems.Where(oi => oi.OrderId == item.OrderId).ToListAsync()).Sum(i => i.TotalPrice);
                order.TotalAmount = order.SubTotal - order.DiscountAmount;
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> RemoveOrderItem(int itemId)
    {
        var item = await _db.OrderItems.FindAsync(itemId);
        if (item == null) return Ok();

        var order = await _db.Orders.FindAsync(item.OrderId);
        _db.OrderItems.Remove(item);

        if (order != null)
        {
            order.SubTotal = (await _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToListAsync()).Sum(i => i.TotalPrice);
            order.TotalAmount = order.SubTotal - order.DiscountAmount;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitOrder(int orderId, string? notes)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.Status = OrderStatus.Submitted;
        order.SubmittedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(notes))
            order.Notes = notes;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đơn đã gửi bếp.";
        return RedirectToAction(nameof(OrderItems), new { orderId });
    }

    // ════════════════════════════════════════════════════════════════════
    // PAYMENT (Thanh toán)
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Payment(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCash(int orderId, string? notes)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = order.TotalAmount,
            Method = PaymentMethod.Cash,
            Status = PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow,
            ProcessedByStaffId = userId,
            Notes = notes
        };
        _db.Payments.Add(payment);

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;

        var table = await _db.Tables.FindAsync(order.TableId);
        if (table != null) table.Status = TableStatus.Cleaning;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Thanh toán tiền mặt thành công!";
        return RedirectToAction(nameof(FloorPlan), new { venueId = order.VenueId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayPayOS(int orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        // PayOS integration placeholder
        // In production, call PayOS API here to create payment link
        // For demo: create pending payment and redirect
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = order.TotalAmount,
            Method = PaymentMethod.PayOS,
            Status = PaymentStatus.Pending,
            PaymentUrl = $"https://payos.com/demo?order={orderId}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // In production: redirect to PayOS checkout
        // For demo: show payment URL
        TempData["PayOSUrl"] = payment.PaymentUrl;
        TempData["PayOSOrderId"] = orderId;
        return RedirectToAction(nameof(Payment), new { orderId });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayOSPayment(int orderId)
    {
        // Called by PayOS webhook or staff confirms manually
        var payment = await _db.Payments
            .Where(p => p.OrderId == orderId && p.Method == PaymentMethod.PayOS && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var order = await _db.Orders.FindAsync(orderId);
        if (payment != null && order != null)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;

            var table = await _db.Tables.FindAsync(order.TableId);
            if (table != null) table.Status = TableStatus.Cleaning;

            await _db.SaveChangesAsync();
        }

        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> ApplyDiscount(int orderId, decimal discountAmount)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return NotFound();

        order.DiscountAmount = discountAmount;
        order.TotalAmount = order.SubTotal + order.TaxAmount - discountAmount;
        await _db.SaveChangesAsync();

        return Json(new { totalAmount = order.TotalAmount });
    }

    // ════════════════════════════════════════════════════════════════════
    // RESERVATION MANAGEMENT (Staff xem & xử lý đặt trước)
    // ════════════════════════════════════════════════════════════════════

    public async Task<IActionResult> Reservations(int? venueId)
    {
        int? vid = venueId ?? await GetAssignedVenueId();
        if (vid == null) return RedirectToAction("SelectVenue", "Owner");

        var venue = await _db.Venues.FindAsync(vid.Value);
        if (venue == null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var reservations = await _db.Reservations
            .Include(r => r.Table)
            .Where(r => r.VenueId == vid.Value
                && r.ReservationTime.Date == today
                && r.Status != ReservationStatus.Cancelled
                && r.Status != ReservationStatus.Completed)
            .OrderBy(r => r.ReservationTime)
            .ToListAsync();

        ViewBag.VenueId = vid.Value;
        ViewBag.VenueName = venue.Name;
        return View(reservations);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmReservation(int id)
    {
        var res = await _db.Reservations.FindAsync(id);
        if (res == null) return NotFound();

        res.Status = ReservationStatus.Confirmed;
        res.ConfirmedAt = DateTime.UtcNow;

        var table = await _db.Tables.FindAsync(res.TableId);
        if (table != null) table.Status = TableStatus.Reserved;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xác nhận đặt trước.";
        return RedirectToAction(nameof(Reservations), new { venueId = res.VenueId });
    }

    [HttpPost]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var res = await _db.Reservations.FindAsync(id);
        if (res == null) return NotFound();

        res.Status = ReservationStatus.Cancelled;
        res.CancelledAt = DateTime.UtcNow;

        var table = await _db.Tables.FindAsync(res.TableId);
        if (table != null && table.Status == TableStatus.Reserved)
            table.Status = TableStatus.Available;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã hủy đặt trước.";
        return RedirectToAction(nameof(Reservations), new { venueId = res.VenueId });
    }

    // ════════════════════════════════════════════════════════════════════
    // TABLE ACTIONS
    // ════════════════════════════════════════════════════════════════════

    [HttpPost]
    public async Task<IActionResult> MarkTableAvailable(int tableId)
    {
        var table = await _db.Tables.FindAsync(tableId);
        if (table == null) return NotFound();

        // Clear any active orders
        var activeOrders = await _db.Orders
            .Where(o => o.TableId == tableId && (o.Status == OrderStatus.Open || o.Status == OrderStatus.Submitted || o.Status == OrderStatus.Served))
            .ToListAsync();

        table.Status = TableStatus.Available;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FloorPlan), new { venueId = table.VenueId });
    }

    [HttpPost]
    public async Task<IActionResult> MarkTableCleaning(int tableId)
    {
        var table = await _db.Tables.FindAsync(tableId);
        if (table == null) return NotFound();

        table.Status = TableStatus.Cleaning;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(FloorPlan), new { venueId = table.VenueId });
    }

    [HttpPost]
    public async Task<IActionResult> OpenNewOrder(int tableId)
    {
        var table = await _db.Tables.FindAsync(tableId);
        if (table == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var order = new Order
        {
            TableId = tableId,
            VenueId = table.VenueId,
            Status = OrderStatus.Open,
            StaffId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);

        table.Status = TableStatus.Occupied;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(OrderItems), new { orderId = order.Id });
    }
}
