namespace MobieleTijdkaart.Application.DTOs;

public class CreateProjectRequest
{
    public string Naam { get; set; } = string.Empty;
    public string? Klantnaam { get; set; }
    public decimal Uurtarief { get; set; }
}

public class UpdateProjectRequest
{
    public string Naam { get; set; } = string.Empty;
    public string? Klantnaam { get; set; }
    public decimal Uurtarief { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string? Klantnaam { get; set; }
    public decimal Uurtarief { get; set; }
}
