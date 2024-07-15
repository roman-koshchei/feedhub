using System.Security.Claims;

namespace Web.Lib;

public static class AuthExtensions
{
    public static string Id(this ClaimsPrincipal claims)
    {
        return claims.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new KeyNotFoundException(
                "User Id isn't present. You must use the method only in Authenticated routes."
            );
    }
}