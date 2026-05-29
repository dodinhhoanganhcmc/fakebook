using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fakebook.Server.Auth;

public static class CurrentUser
{
    public static Guid Id(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("Missing or invalid sub claim");
    }
}
