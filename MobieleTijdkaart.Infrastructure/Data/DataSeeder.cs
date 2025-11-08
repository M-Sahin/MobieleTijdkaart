using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MobieleTijdkaart.Domain.Entities;

namespace MobieleTijdkaart.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedProjectDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Controle: Zijn er al projecten in de database?
        var projectsExist = await dbContext.Projecten.AnyAsync();
        
        if (projectsExist)
        {
            // Er zijn al projecten, dus we hoeven niet te seeden
            return;
        }

        // Haal de eerste gebruiker op uit de database
        var firstUser = await userManager.Users.FirstOrDefaultAsync();
        
        if (firstUser == null)
        {
            // Geen gebruiker gevonden, we kunnen geen projecten seeden
            Console.WriteLine("Warning: No users found in database. Cannot seed projects.");
            return;
        }

        // Maak twee standaard projecten aan
        var projects = new List<Project>
        {
            new Project
            {
                Naam = "Intern Beheer & Administratie",
                Klantnaam = "Intern",
                Uurtarief = 0,
                IsActief = true
            },
            new Project
            {
                Naam = "Klant ABC - Website Migratie",
                Klantnaam = "Klant ABC",
                Uurtarief = 75,
                IsActief = true
            }
        };

        await dbContext.Projecten.AddRangeAsync(projects);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Successfully seeded {projects.Count} projects for user {firstUser.Email}");
    }
}
