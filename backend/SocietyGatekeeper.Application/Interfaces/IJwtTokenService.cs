using SocietyGatekeeper.Domain.Entities;

namespace SocietyGatekeeper.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}
