namespace MobieleTijdkaart.Domain.Entities;

public class TijdRegistratie
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset StartTijd { get; set; }
    public DateTimeOffset? EindTijd { get; set; }
    public int DuurInMinuten { get; set; }
    public string? Omschrijving { get; set; }
    
    // Navigation properties
    public Project Project { get; set; } = null!;
}
