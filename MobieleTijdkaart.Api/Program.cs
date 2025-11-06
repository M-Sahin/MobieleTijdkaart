using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobieleTijdkaart.Application.DTOs;
using MobieleTijdkaart.Domain.Entities;
using MobieleTijdkaart.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Database configuratie
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity configuratie
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT configuratie
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "SuperGeheimeSleutelVoorJWT123!@#MinimaalDertigTekens";
var issuer = jwtSettings["Issuer"] ?? "MobieleTijdkaartApi";
var audience = jwtSettings["Audience"] ?? "MobieleTijdkaartClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// CORS configuratie
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger/OpenAPI configuratie
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mobiele Tijdkaart API",
        Version = "v1",
        Description = "API voor uren- en rittenregistratie"
    });

    // JWT Bearer token configuratie in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowNextJs");
app.UseAuthentication();
app.UseAuthorization();

// ============================================
// AUTH ENDPOINTS
// ============================================

app.MapPost("/api/auth/register", async (
    RegisterRequestDto request,
    UserManager<IdentityUser> userManager) =>
{
    if (request.Password != request.ConfirmPassword)
    {
        return Results.BadRequest(new { message = "Wachtwoorden komen niet overeen" });
    }

    var user = new IdentityUser
    {
        UserName = request.Email,
        Email = request.Email
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.BadRequest(new { errors = result.Errors });
    }

    return Results.Ok(new { message = "Gebruiker succesvol aangemaakt" });
})
.WithName("Register")
.WithTags("Authentication");

app.MapPost("/api/auth/login", async (
    LoginRequestDto request,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    // Genereer JWT token
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email!),
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email!),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiresAt = DateTime.UtcNow.AddHours(8);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: expiresAt,
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new LoginResponseDto
    {
        Token = tokenString,
        UserId = user.Id,
        Email = user.Email!,
        ExpiresAt = expiresAt
    });
})
.WithName("Login")
.WithTags("Authentication");

// ============================================
// PROJECTEN ENDPOINTS
// ============================================

app.MapPost("/api/projecten", async (
    CreateProjectDto dto,
    ApplicationDbContext db,
    ClaimsPrincipal user) =>
{
    var project = new Project
    {
        Naam = dto.Naam,
        Klantnaam = dto.Klantnaam,
        Uurtarief = dto.Uurtarief,
        IsActief = true
    };

    db.Projecten.Add(project);
    await db.SaveChangesAsync();

    return Results.Created($"/api/projecten/{project.Id}", new ProjectDto
    {
        Id = project.Id,
        Naam = project.Naam,
        Klantnaam = project.Klantnaam,
        Uurtarief = project.Uurtarief,
        IsActief = project.IsActief
    });
})
.RequireAuthorization()
.WithName("CreateProject")
.WithTags("Projecten");

app.MapGet("/api/projecten", async (
    ApplicationDbContext db,
    ClaimsPrincipal user) =>
{
    var projecten = await db.Projecten
        .Where(p => p.IsActief)
        .Select(p => new ProjectDto
        {
            Id = p.Id,
            Naam = p.Naam,
            Klantnaam = p.Klantnaam,
            Uurtarief = p.Uurtarief,
            IsActief = p.IsActief
        })
        .ToListAsync();

    return Results.Ok(projecten);
})
.RequireAuthorization()
.WithName("GetProjects")
.WithTags("Projecten");

// ============================================
// TIJDREGISTRATIE ENDPOINTS
// ============================================

app.MapPost("/api/tijd", async (
    CreateTijdRegistratieDto dto,
    ApplicationDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    // Valideer dat project bestaat
    var projectExists = await db.Projecten.AnyAsync(p => p.Id == dto.ProjectId);
    if (!projectExists)
    {
        return Results.BadRequest(new { message = "Project niet gevonden" });
    }

    var tijdRegistratie = new TijdRegistratie
    {
        ProjectId = dto.ProjectId,
        UserId = userId,
        StartTijd = dto.StartTijd,
        EindTijd = dto.EindTijd,
        DuurInMinuten = dto.DuurInMinuten,
        Omschrijving = dto.Omschrijving
    };

    db.TijdRegistraties.Add(tijdRegistratie);
    await db.SaveChangesAsync();

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
})
.RequireAuthorization()
.WithName("CreateTijdRegistratie")
.WithTags("Tijdregistratie");

app.MapGet("/api/tijd/project/{projectId}", async (
    int projectId,
    ApplicationDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
})
.RequireAuthorization()
.WithName("GetTijdRegistratiesByProject")
.WithTags("Tijdregistratie");

app.MapGet("/api/tijd", async (
    ApplicationDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
})
.RequireAuthorization()
.WithName("GetAllTijdRegistraties")
.WithTags("Tijdregistratie");

app.Run();
