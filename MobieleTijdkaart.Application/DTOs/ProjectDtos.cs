namespace MobieleTijdkaart.Application.DTOs;

public class CreateProjectDto
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
    public bool IsActief { get; set; }
}
