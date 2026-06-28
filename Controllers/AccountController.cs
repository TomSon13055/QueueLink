using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Integrations.Auth;
using QueueLink.Integrations.Session;
using QueueLink.Models;
using QueueLink.Services;
using QueueLink.ViewModels;

namespace QueueLink.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly ISupabaseAuthService _auth;
    private readonly IGuestSession _guestSession;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        ISupabaseAuthService auth,
        IGuestSession guestSession,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _auth = auth;
        _guestSession = guestSession;
        _logger = logger;
    }

    // ── Login (local password) ────────────────────────────────────────

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

    // ── Magic Link Login ───────────────────────────────────────────────

    [HttpGet]
    public IActionResult MagicLinkLogin()
    {
        return View(new MagicLinkLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MagicLinkLogin(MagicLinkLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var normalized = model.Email.Trim().ToLowerInvariant();
        var result = await _auth.SendMagicLinkAsync(normalized);

        if (!result.Ok)
        {
            // Vẫn hiển thị thành công để tránh email enumeration
            _logger.LogWarning("[MagicLink] Send failed for {Email}: {Error}", normalized, result.Error);
        }

        TempData["Info"] = "Nếu email tồn tại trong hệ thống, một liên kết đăng nhập đã được gửi. Vui lòng kiểm tra hộp thư (kể cả Spam).";
        return RedirectToAction(nameof(Login));
    }

    // ── Logout ─────────────────────────────────────────────────────────

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

    // ── Register (Supabase Auth — email confirmation) ───────────────────

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

        var normalized = model.Email.Trim().ToLowerInvariant();

        // Kiểm tra user local đã tồn tại chưa.
        var existing = await _userManager.FindByEmailAsync(normalized);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký. Vui lòng đăng nhập.");
            return View(model);
        }

        var result = await _auth.SignUpAsync(normalized, model.Password, model.FullName);

        if (!result.Ok)
        {
            // Supabase trả lỗi chi tiết: user_exists, weak_password, ...
            ModelState.AddModelError("", result.Error ?? "Đăng ký thất bại. Vui lòng thử lại.");
            return View(model);
        }

        _logger.LogInformation("[Register] Supabase sign-up sent confirmation for {Email}", normalized);

        TempData["Info"] = $"Đã gửi liên kết xác nhận tới {normalized}. Vui lòng kiểm tra hộp thư (kể cả Spam) để hoàn tất đăng ký.";
        return RedirectToAction(nameof(Login));
    }

    // ── Confirm Email (called when user clicks link in Supabase email) ─

    /// <summary>
    /// Endpoint để xử lý email confirmation từ Supabase.
    /// URL: /Account/ConfirmEmail?token=xxx
    /// Sau khi exchange token thành công, user được đăng nhập local.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string token, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Liên kết không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _auth.ExchangeTokenAsync(token);

        if (!result.Ok)
        {
            _logger.LogWarning("[ConfirmEmail] ExchangeToken failed: {Error}", result.Error);
            TempData["Error"] = result.Error ?? "Xác nhận email thất bại. Liên kết có thể đã hết hạn.";
            return RedirectToAction(nameof(Login));
        }

        // Sync guest session sau khi đăng nhập.
        if (result.Email != null)
            await SyncGuestSessionAsync(result.Email);

        TempData["Success"] = "Xác nhận email thành công! Chào mừng bạn đến với QueueLink.";
        return RedirectToAction("Index", "Home");
    }

    // ── Helpers ────────────────────────────────────────────────────────

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

public class MagicLinkLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;
}
