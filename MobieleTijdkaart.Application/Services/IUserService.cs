using System.Security.Claims;

namespace MobieleTijdkaart.Application.Services;

public interface IUserService
{
    string? GetCurrentUserId(ClaimsPrincipal user);
}
