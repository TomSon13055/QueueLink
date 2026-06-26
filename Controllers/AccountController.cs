using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
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
    private readonly IOtpService _otp;
    private readonly IGuestSession _guestSession;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOtpService otp,
        IGuestSession guestSession,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _otp = otp;
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

        var result = await _signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: false);

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

    // ── Register (Customer only) + OTP verification ──────────────────

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
            if (existing.EmailConfirmed)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký. Vui lòng đăng nhập.");
                return View(model);
            }
            // User đã tạo nhưng chưa xác nhận OTP — xóa để đăng ký lại sạch sẽ.
            await _userManager.DeleteAsync(existing);
        }

        // Tạo user mới, KHÔNG đăng nhập ngay — phải xác thực OTP trước.
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = model.FullName,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
                ModelState.AddModelError("", err.Description);
            return View(model);
        }

        // Gán role Customer.
        await _userManager.AddToRoleAsync(user, "Customer");

        // Tạo CustomerProfile ngay để auto-fill form lấy số sau này.
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

        // Sinh và gửi OTP. Nếu gửi thất bại (Gmail chưa cấu hình / timeout),
        // user vẫn được tạo nhưng cần đợi admin hỗ trợ xác thực email.
        try
        {
            await _otp.GenerateAndSendAsync(normalizedEmail, model.FullName);
            TempData["Info"] = $"Mã OTP đã được gửi tới {normalizedEmail}. Vui lòng kiểm tra hộp thư (kể cả thư mục Spam).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Register] Failed to send OTP for {Email}", normalizedEmail);
            TempData["Warning"] = $"Không thể gửi mã OTP tự động. Vui lòng liên hệ hỗ trợ để xác thực tài khoản.";
        }

        return RedirectToAction(nameof(VerifyOtp), new { email = normalizedEmail });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Register));

        return View(new VerifyOtpViewModel { Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var ok = await _otp.VerifyAsync(normalizedEmail, model.Code);

        if (!ok)
        {
            ModelState.AddModelError("", "Mã OTP không đúng hoặc đã hết hạn.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null)
        {
            ModelState.AddModelError("", "Không tìm thấy tài khoản.");
            return View(model);
        }

        // Đánh dấu email đã xác thực + đăng nhập luôn.
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: true);

        await SyncGuestSessionAsync(normalizedEmail);

        TempData["Success"] = "Xác thực email thành công! Chào mừng bạn đến với QueueLink.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(Register));

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null)
        {
            TempData["Error"] = "Email chưa được đăng ký.";
            return RedirectToAction(nameof(Register));
        }

        try
        {
            await _otp.ResendAsync(normalizedEmail, user.FullName ?? "");
            TempData["Info"] = $"Đã gửi lại mã OTP tới {normalizedEmail}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(VerifyOtp), new { email = normalizedEmail });
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Lưu thông tin guest vào session để auto-fill form lấy số ở những lần sau.
    /// </summary>
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
