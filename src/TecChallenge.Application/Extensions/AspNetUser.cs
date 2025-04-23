using System.Security.Claims;
using TecChallenge.Domain.Interfaces;

namespace TecChallenge.Application.Extensions;

public class AspNetUser(IHttpContextAccessor accessor) : IUser
{
    public string Name => accessor.HttpContext.User.Identity.Name;

    public Guid GetUserId()
    {
        return IsAuthenticated()
            ? Guid.Parse(accessor.HttpContext.User.GetUserId())
            : Guid.Empty;
    }

    public string GetUserEmail()
    {
        return IsAuthenticated() ? accessor.HttpContext.User.GetUserEmail() : "";
    }

    public bool IsAuthenticated()
    {
        return accessor.HttpContext.User.Identity.IsAuthenticated;
    }

    public bool IsInRole(string role)
    {
        return accessor.HttpContext.User.IsInRole(role);
    }

    public IEnumerable<Claim> GetClaimsIdentity()
    {
        return accessor.HttpContext.User.Claims;
    }

    public ClaimsIdentity? GetUserIdentity()
    {
        return accessor.HttpContext.User.Identity as ClaimsIdentity;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null) throw new ArgumentException(null, nameof(principal));

        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }

    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        if (principal == null) throw new ArgumentException(null, nameof(principal));

        var claim = principal.FindFirst(ClaimTypes.Email);
        return claim?.Value;
    }
}