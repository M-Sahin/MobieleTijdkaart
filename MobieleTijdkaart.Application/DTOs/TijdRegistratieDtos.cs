namespace MobieleTijdkaart.Application.DTOs;

public record CreateTijdRegistratieRequest(
    int ProjectId,
    DateTimeOffset StartTijd,
    string? Omschrijving
);

public class TijdRegistratieDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectNaam { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset StartTijd { get; set; }
    public DateTimeOffset? EindTijd { get; set; }
    public int DuurInMinuten { get; set; }
    public string? Omschrijving { get; set; }
}
