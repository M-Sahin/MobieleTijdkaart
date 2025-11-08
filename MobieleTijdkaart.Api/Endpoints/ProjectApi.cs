using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MobieleTijdkaart.Application.DTOs;
using MobieleTijdkaart.Application.Services;
using MobieleTijdkaart.Domain.Entities;
using MobieleTijdkaart.Infrastructure.Data;

namespace MobieleTijdkaart.Api.Endpoints;

public static class ProjectApi
{
    public static RouteGroupBuilder MapProjectApi(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllProjectenAsync)
            .RequireAuthorization()
            .WithName("GetAllProjecten")
            .WithTags("Projecten")
            .WithOpenApi();

        group.MapPost("/", CreateProjectAsync)
            .RequireAuthorization()
            .WithName("CreateProject")
            .WithTags("Projecten")
            .WithOpenApi();

        group.MapPut("/{id}", UpdateProjectAsync)
            .RequireAuthorization()
            .WithName("UpdateProject")
            .WithTags("Projecten")
            .WithOpenApi();

        group.MapDelete("/{id}", DeleteProjectAsync)
            .RequireAuthorization()
            .WithName("DeleteProject")
            .WithTags("Projecten")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> GetAllProjectenAsync(
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var projecten = await db.Projecten
            .Where(p => p.UserId == userId)
            .Select(p => new ProjectDto
            {
                Id = p.Id,
                Naam = p.Naam,
                Klantnaam = p.Klantnaam,
                Uurtarief = p.Uurtarief
            })
            .ToListAsync();

        return Results.Ok(projecten);
    }

    private static async Task<IResult> CreateProjectAsync(
        CreateProjectRequest request,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                detail: "Gebruiker kon niet worden geïdentificeerd uit de token.",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        // Valideer de request
        if (string.IsNullOrWhiteSpace(request.Naam))
        {
            return Results.BadRequest(new { message = "Projectnaam is verplicht." });
        }

        // Maak nieuwe project met de UserId van de ingelogde gebruiker
        var project = new Project
        {
            Naam = request.Naam,
            UserId = userId,
            IsActief = true,
            Klantnaam = request.Klantnaam,
            Uurtarief = request.Uurtarief
        };

        db.Projecten.Add(project);
        await db.SaveChangesAsync();

        var result = new ProjectDto
        {
            Id = project.Id,
            Naam = project.Naam,
            Klantnaam = project.Klantnaam,
            Uurtarief = project.Uurtarief
        };

        return Results.Created($"/api/projecten/{result.Id}", result);
    }

    private static async Task<IResult> UpdateProjectAsync(
        int id,
        UpdateProjectRequest request,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Valideer de request
        if (string.IsNullOrWhiteSpace(request.Naam))
        {
            return Results.BadRequest(new { message = "Projectnaam is verplicht." });
        }

        // Zoek het project op
        var project = await db.Projecten
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return Results.NotFound(new { message = "Project niet gevonden." });
        }

        // Controleer eigendom - behoort dit project tot de ingelogde gebruiker?
        if (project.UserId != userId)
        {
            return Results.Problem(
                detail: "U heeft geen toegang tot dit project.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        // Update het project
        project.Naam = request.Naam;
        project.Klantnaam = request.Klantnaam;
        project.Uurtarief = request.Uurtarief;
        await db.SaveChangesAsync();

        var result = new ProjectDto
        {
            Id = project.Id,
            Naam = project.Naam,
            Klantnaam = project.Klantnaam,
            Uurtarief = project.Uurtarief
        };

        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteProjectAsync(
        int id,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Zoek het project op
        var project = await db.Projecten
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return Results.NotFound(new { message = "Project niet gevonden." });
        }

        // Controleer eigendom - behoort dit project tot de ingelogde gebruiker?
        if (project.UserId != userId)
        {
            return Results.Problem(
                detail: "U heeft geen toegang tot dit project.",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        // Verwijder het project
        db.Projecten.Remove(project);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
