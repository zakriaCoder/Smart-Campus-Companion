using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartCampus.Services;

/// <summary>
/// AuthService only handles logout.
/// Login is handled by the /auth/signin minimal API endpoint in Program.cs
/// because HttpContext is null inside Blazor Server interactive components.
/// </summary>
public class AuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogoutAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is not null)
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}