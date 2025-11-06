namespace MobieleTijdkaart.Domain.Entities;

public class RitRegistratie
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public DateOnly Datum { get; set; }
    public string StartAdres { get; set; } = string.Empty;
    public string EindAdres { get; set; } = string.Empty;
    public decimal GeredenKilometers { get; set; }
    public string? Doel { get; set; }
    
    // Navigation properties
    public Project? Project { get; set; }
}
