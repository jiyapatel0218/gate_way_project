using System.Security.Claims;

namespace SocietyGatekeeper.API.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    public static Guid? GetSocietyId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("societyId");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
