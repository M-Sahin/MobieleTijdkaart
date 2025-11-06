using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MobieleTijdkaart.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        // Zorg ervoor dat de database bestaat
        await context.Database.MigrateAsync();

        // Controleer of er al gebruikers zijn
        if (await context.Users.AnyAsync())
        {
            return; // Database is al geseeded
        }

        // Maak testgebruiker aan
        var testUser = new IdentityUser
        {
            UserName = "testuser@app.nl",
            Email = "testuser@app.nl",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(testUser, "Wachtwoord123!");

        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
