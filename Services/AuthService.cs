using System.Security.Claims;
using AuraLiving.Models;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuraLiving.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest, HttpContext httpContext);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest, HttpContext httpContext);
    Task LogoutAsync(HttpContext httpContext);
    Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
        {
            return new AuthResponseDto { Success = false, Message = "Email and password are required." };
        }

        var user = await _userRepository.GetByEmailAsync(loginRequest.Email.Trim());
        if (user == null || (user.PasswordHash != loginRequest.Password && user.PasswordHash != HashPassword(loginRequest.Password)))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid email address or password." };
        }

        if (user.IsBlocked)
        {
            return new AuthResponseDto { Success = false, Message = "Your account has been suspended by an Administrator. Please contact support." };
        }

        // Generate JWT Token & Store securely in HttpOnly Cookie
        IssueJwtCookie(user, httpContext);
        await SignInUserClaimsAsync(user, httpContext);

        string redirectUrl = user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "/admin" : "/account";

        return new AuthResponseDto
        {
            Success = true,
            Message = $"Welcome back, {user.FullName}!",
            RedirectUrl = redirectUrl,
            User = user
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(registerRequest.Email) || string.IsNullOrWhiteSpace(registerRequest.Password))
        {
            return new AuthResponseDto { Success = false, Message = "Email and password are required." };
        }

        var existingUser = await _userRepository.GetByEmailAsync(registerRequest.Email.Trim());
        if (existingUser != null)
        {
            return new AuthResponseDto { Success = false, Message = "An account with this email address already exists." };
        }

        var newUser = new UserModel
        {
            Id = "USR-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            FullName = registerRequest.FullName.Trim(),
            Email = registerRequest.Email.Trim(),
            PasswordHash = registerRequest.Password,
            Phone = registerRequest.Phone?.Trim() ?? "",
            Role = "Customer", // Enforce standard customer role on public registration
            CreatedAt = DateTime.UtcNow
        };

        bool created = await _userRepository.CreateUserAsync(newUser);
        if (!created)
        {
            return new AuthResponseDto { Success = false, Message = "Failed to create user account." };
        }

        // Generate JWT Token & Store securely in HttpOnly Cookie
        IssueJwtCookie(newUser, httpContext);
        await SignInUserClaimsAsync(newUser, httpContext);

        string redirectUrl = newUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "/admin" : "/account";

        return new AuthResponseDto
        {
            Success = true,
            Message = "Account registered successfully!",
            RedirectUrl = redirectUrl,
            User = newUser
        };
    }

    public async Task LogoutAsync(HttpContext httpContext)
    {
        // Delete JWT Token Cookie permanently on Logout
        httpContext.Response.Cookies.Delete("IndoriaJwtToken");
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private void IssueJwtCookie(UserModel user, HttpContext httpContext)
    {
        string token = _jwtTokenService.GenerateToken(user);
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Hide from client-side JavaScript for security against XSS
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            IsEssential = true
        };
        httpContext.Response.Cookies.Append("IndoriaJwtToken", token, cookieOptions);
    }

    public async Task<UserModel?> GetCurrentUserAsync(ClaimsPrincipal userPrincipal)
    {
        var emailClaim = userPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(emailClaim)) return null;
        return await _userRepository.GetByEmailAsync(emailClaim);
    }

    private async Task SignInUserClaimsAsync(UserModel user, HttpContext httpContext)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
    }

    private string HashPassword(string password) => password;
}
