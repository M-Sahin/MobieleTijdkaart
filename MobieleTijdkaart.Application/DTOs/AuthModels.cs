namespace MobieleTijdkaart.Application.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token);

public record RegisterRequest(string Email, string Wachtwoord, string WachtwoordBevestiging);
