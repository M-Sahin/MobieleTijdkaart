using System.Security.Claims;

namespace MobieleTijdkaart.Application.Services;

public class UserService : IUserService
{
    public string? GetCurrentUserId(ClaimsPrincipal user)
    {
        // Prioriteit: eerst ClaimTypes.NameIdentifier, dan "sub" claim
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            userId = user.FindFirst("sub")?.Value;
        }
        
        return userId;
    }
}
