namespace MobieleTijdkaart.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string? Klantnaam { get; set; }
    public decimal Uurtarief { get; set; }
    public bool IsActief { get; set; } = true;
    
    // Navigation properties
    public ICollection<TijdRegistratie> TijdRegistraties { get; set; } = new List<TijdRegistratie>();
    public ICollection<RitRegistratie> RitRegistraties { get; set; } = new List<RitRegistratie>();
}
