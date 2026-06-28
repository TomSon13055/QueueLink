using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueLink.Data;
using QueueLink.Models;
using Supabase;

namespace QueueLink.Integrations.Auth;

public interface ISupabaseAuthService
{
    Task<AuthResult> SignUpAsync(string email, string password, string fullName, CancellationToken ct = default);
    Task<AuthResult> SendMagicLinkAsync(string email, CancellationToken ct = default);
    Task<AuthResult> ExchangeTokenAsync(string accessToken, CancellationToken ct = default);
    SupabaseUserInfo? GetUserFromToken(string accessToken);
}

public record AuthResult(bool Ok, string? Error = null, ClaimsPrincipal? Principal = null, string? Email = null);
public record SupabaseUserInfo(string Id, string Email, string? FullName);

public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly Client _client;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(
        Client client,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ILogger<SupabaseAuthService> logger)
    {
        _client = client;
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _logger = logger;
    }

    public async Task<AuthResult> SignUpAsync(string email, string password, string fullName, CancellationToken ct = default)
    {
        try
        {
            var session = await _client.Auth.SignUp(email, password);

            _logger.LogInformation("[SupabaseAuth] SignUp sent confirmation for {Email}", email);
            return new AuthResult(true, Email: email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SupabaseAuth] SignUp failed for {Email}", email);
            return new AuthResult(false, SimplifyError(ex));
        }
    }

    public async Task<AuthResult> SendMagicLinkAsync(string email, CancellationToken ct = default)
    {
        try
        {
            await _client.Auth.SignInWithOtp(new Supabase.Gotrue.SignInWithPasswordlessEmailOptions(email));

            _logger.LogInformation("[SupabaseAuth] Magic link sent to {Email}", email);
            return new AuthResult(true, Email: email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SupabaseAuth] SendMagicLink failed for {Email}", email);
            return new AuthResult(false, SimplifyError(ex));
        }
    }

    public async Task<AuthResult> ExchangeTokenAsync(string accessToken, CancellationToken ct = default)
    {
        try
        {
            var session = await _client.Auth.SetSession(accessToken, null);
            var sbUser = session.User;
            if (string.IsNullOrEmpty(sbUser.Email))
                return new AuthResult(false, "Token không chứa email hợp lệ.");

            var localUser = await _userManager.FindByEmailAsync(sbUser.Email);
            if (localUser == null)
            {
                localUser = new ApplicationUser
                {
                    UserName = sbUser.Email,
                    Email = sbUser.Email,
                    FullName = sbUser.UserMetadata?.TryGetValue("full_name", out var name) == true ? name?.ToString() : null,
                    EmailConfirmed = true,
                    SupabaseId = sbUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(localUser);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    return new AuthResult(false, $"Lỗi tạo tài khoản: {errors}");
                }

                await _userManager.AddToRoleAsync(localUser, "Customer");

                var profile = new CustomerProfile
                {
                    UserId = localUser.Id,
                    FullName = localUser.FullName ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.CustomerProfiles.Add(profile);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                localUser.SupabaseId = sbUser.Id;
                await _userManager.UpdateAsync(localUser);
            }

            await _signInManager.SignInAsync(localUser, isPersistent: true);

            _logger.LogInformation("[SupabaseAuth] ExchangeToken succeeded for {Email}", sbUser.Email);
            return new AuthResult(true, Principal: null, Email: sbUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SupabaseAuth] ExchangeToken failed");
            return new AuthResult(false, SimplifyError(ex));
        }
    }

    public SupabaseUserInfo? GetUserFromToken(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(accessToken) as JwtSecurityToken;

            var sub = jsonToken?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jsonToken?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jsonToken?.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
                return null;

            return new SupabaseUserInfo(sub, email, name);
        }
        catch
        {
            return null;
        }
    }

    private static string SimplifyError(Exception ex)
    {
        if (ex.InnerException != null)
            return ex.InnerException.Message;
        return ex.Message;
    }
}
