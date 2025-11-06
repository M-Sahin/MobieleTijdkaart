using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MobieleTijdkaart.Application.DTOs;
using MobieleTijdkaart.Application.Services;
using MobieleTijdkaart.Domain.Entities;
using MobieleTijdkaart.Infrastructure.Data;

namespace MobieleTijdkaart.Api.Endpoints;

public static class TijdApi
{
    public static RouteGroupBuilder MapTijdApi(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateTijdRegistratieAsync)
            .RequireAuthorization()
            .WithName("CreateTijdRegistratie")
            .WithTags("Tijdregistratie")
            .WithOpenApi();

        group.MapPut("/{id}/stop", StopTijdRegistratieAsync)
            .RequireAuthorization()
            .WithName("StopTijdRegistratie")
            .WithTags("Tijdregistratie")
            .WithOpenApi();

        group.MapGet("/", GetAllTijdRegistratiesAsync)
            .RequireAuthorization()
            .WithName("GetAllTijdRegistraties")
            .WithTags("Tijdregistratie")
            .WithOpenApi();

        group.MapGet("/project/{projectId}", GetTijdRegistratiesByProjectAsync)
            .RequireAuthorization()
            .WithName("GetTijdRegistratiesByProject")
            .WithTags("Tijdregistratie")
            .WithOpenApi();

        return group;
    }

    private static async Task<IResult> CreateTijdRegistratieAsync(
        CreateTijdRegistratieRequest request,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        // Haal de ingelogde gebruiker op uit de JWT token
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Problem(
                detail: "Gebruiker kon niet worden geïdentificeerd uit de token.",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        // Valideer dat project bestaat
        var projectExists = await db.Projecten.AnyAsync(p => p.Id == request.ProjectId);
        if (!projectExists)
        {
            return Results.Problem(
                detail: "Project niet gevonden.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        // Maak nieuwe tijdregistratie met de UserId van de ingelogde gebruiker
        var tijdRegistratie = new TijdRegistratie
        {
            ProjectId = request.ProjectId,
            UserId = userId, // Server-side ingevuld!
            StartTijd = request.StartTijd,
            EindTijd = null,
            DuurInMinuten = 0,
            Omschrijving = request.Omschrijving
        };

        db.TijdRegistraties.Add(tijdRegistratie);
        await db.SaveChangesAsync();

        // Haal het opgeslagen record op met project informatie
        var result = await db.TijdRegistraties
            .Include(t => t.Project)
            .Where(t => t.Id == tijdRegistratie.Id)
            .Select(t => new TijdRegistratieDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectNaam = t.Project.Naam,
                UserId = t.UserId,
                StartTijd = t.StartTijd,
                EindTijd = t.EindTijd,
                DuurInMinuten = t.DuurInMinuten,
                Omschrijving = t.Omschrijving
            })
            .FirstAsync();

        return Results.Created($"/api/tijd/{result.Id}", result);
    }

    private static async Task<IResult> StopTijdRegistratieAsync(
        int id,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        // Haal de ingelogde gebruiker op uit de JWT token
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Zoek de tijdregistratie op
        var tijdRegistratie = await db.TijdRegistraties
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tijdRegistratie == null)
        {
            return Results.NotFound(new { message = "Tijdregistratie niet gevonden" });
        }

        // Controle 1: Eigendom - behoort dit record tot de ingelogde gebruiker?
        if (tijdRegistratie.UserId != userId)
        {
            return Results.Problem(
                detail: "U heeft geen toegang tot deze tijdregistratie",
                statusCode: StatusCodes.Status403Forbidden
            );
        }

        // Controle 2: Controleer of de tijdregistratie nog niet is afgesloten
        if (tijdRegistratie.EindTijd.HasValue)
        {
            return Results.BadRequest(new { message = "Deze tijdregistratie is al afgesloten" });
        }

        // Update: Vul EindTijd in met huidige tijd
        tijdRegistratie.EindTijd = DateTimeOffset.UtcNow;

        // Duur Berekening: Bereken DuurInMinuten
        var duur = tijdRegistratie.EindTijd.Value - tijdRegistratie.StartTijd;
        tijdRegistratie.DuurInMinuten = (int)duur.TotalMinutes;

        // Sla wijzigingen op
        await db.SaveChangesAsync();

        // Retourneer bijgewerkte registratie
        var result = new TijdRegistratieDto
        {
            Id = tijdRegistratie.Id,
            ProjectId = tijdRegistratie.ProjectId,
            ProjectNaam = tijdRegistratie.Project.Naam,
            UserId = tijdRegistratie.UserId,
            StartTijd = tijdRegistratie.StartTijd,
            EindTijd = tijdRegistratie.EindTijd,
            DuurInMinuten = tijdRegistratie.DuurInMinuten,
            Omschrijving = tijdRegistratie.Omschrijving
        };

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAllTijdRegistratiesAsync(
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var registraties = await db.TijdRegistraties
            .Include(t => t.Project)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.StartTijd)
            .Select(t => new TijdRegistratieDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectNaam = t.Project.Naam,
                UserId = t.UserId,
                StartTijd = t.StartTijd,
                EindTijd = t.EindTijd,
                DuurInMinuten = t.DuurInMinuten,
                Omschrijving = t.Omschrijving
            })
            .ToListAsync();

        return Results.Ok(registraties);
    }

    private static async Task<IResult> GetTijdRegistratiesByProjectAsync(
        int projectId,
        ApplicationDbContext db,
        IUserService userService,
        ClaimsPrincipal user)
    {
        var userId = userService.GetCurrentUserId(user);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var registraties = await db.TijdRegistraties
            .Include(t => t.Project)
            .Where(t => t.ProjectId == projectId && t.UserId == userId)
            .OrderByDescending(t => t.StartTijd)
            .Select(t => new TijdRegistratieDto
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                ProjectNaam = t.Project.Naam,
                UserId = t.UserId,
                StartTijd = t.StartTijd,
                EindTijd = t.EindTijd,
                DuurInMinuten = t.DuurInMinuten,
                Omschrijving = t.Omschrijving
            })
            .ToListAsync();

        return Results.Ok(registraties);
    }
}
