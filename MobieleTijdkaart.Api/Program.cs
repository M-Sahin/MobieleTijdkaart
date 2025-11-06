using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MobieleTijdkaart.Api.Endpoints;
using MobieleTijdkaart.Application.Services;
using MobieleTijdkaart.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// CORS politiek naam constante
const string CorsPolicyName = "FrontendPolicy";

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
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
}

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

// Registreer IUserService
builder.Services.AddScoped<IUserService, UserService>();

// CORS configuratie
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",                                    // Lokale Next.js ontwikkeling
                "https://mobiele-tijdkaart-frontend.vercel.app"            // Vercel productie (placeholder)
              )
              .AllowAnyHeader()                                             // Sta alle headers toe (inclusief Authorization)
              .WithMethods("GET", "POST", "PUT", "DELETE")                 // Alleen essentiële HTTP methods
              .AllowCredentials();                                          // Voor toekomstige HTTP-only cookies
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

// Database initialisatie en seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        await DbInitializer.InitializeAsync(context, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicyName);  // CORS middleware moet vóór Authentication en Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints met route groups
var api = app.MapGroup("/api");

api.MapGroup("/auth")
    .MapAuthApi()
    .WithTags("Authentication");

api.MapGroup("/tijd")
    .MapTijdApi()
    .WithTags("Tijdregistratie");

app.Run();
