using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IGuestSession _guestSession;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IGuestSession guestSession,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _guestSession = guestSession;
        _logger = logger;
    }

    // ── Login / Logout ───────────────────────────────────────────────

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            await SyncGuestSessionAsync(email);
            return await RedirectByRoleAsync(returnUrl);
        }

        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _guestSession.Clear();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    // ── Register (Customer only) ─────────────────────────────────────

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        var existing = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký. Vui lòng đăng nhập.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = model.FullName,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
                ModelState.AddModelError("", err.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        var profile = new CustomerProfile
        {
            UserId = user.Id,
            FullName = model.FullName,
            Phone = model.Phone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.CustomerProfiles.Add(profile);
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: true);
        await SyncGuestSessionAsync(normalizedEmail);

        TempData["Success"] = "Đăng ký thành công! Chào mừng bạn đến với QueueLink.";
        return RedirectToAction("Index", "Home");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task SyncGuestSessionAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return;

        var profile = await _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

        _guestSession.Set(new GuestProfile
        {
            FullName = profile?.FullName ?? user.FullName ?? "",
            Phone = profile?.Phone ?? "",
            Email = user.Email ?? email,
            UserId = user.Id
        });
    }

    private async Task<IActionResult> RedirectByRoleAsync(string? returnUrl)
    {
        var email = User.Identity?.Name;
        if (!string.IsNullOrEmpty(email))
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (roles.Contains("Staff"))
                    return RedirectToAction("Index", "StaffQueue");
            }
        }

        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}
