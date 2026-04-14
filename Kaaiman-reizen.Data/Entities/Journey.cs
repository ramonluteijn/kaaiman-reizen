using System.ComponentModel.DataAnnotations;

namespace Kaaiman_reizen.Data.Entities;

public class Journey : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Reis is verplicht.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start datum is verplicht.")]
    public DateOnly Start { get; set; }

    [Required(ErrorMessage = "Eind datum is verplicht.")]
    public DateOnly End { get; set; }

    [Required(ErrorMessage = "Aantal busjes is verplicht.")]
    public int? Busses { get; set; }

    [Required(ErrorMessage = "Aantal reizigers is verplicht.")]
    [Range(1, int.MaxValue, ErrorMessage = "Aantal reizigers moet groter of gelijk aan 1 zijn.")]
    public int? Travelers { get; set; }

    [Required(ErrorMessage = "Boekingsstatus is verplicht.")]
    [Range(1, 3, ErrorMessage = "Boekingsstatus moet 1, 2 of 3 zijn.")]
    public int? BookingStatus { get; set; }

    /// <summary>How many travel leaders must be assigned to this journey. Defaults to 1.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "RequiredLeaders moet minimaal 1 zijn.")]
    public int RequiredLeaders { get; set; } = 1;

    public List<TravelLeader> TravelLeaders { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Start >= End)
        {
            yield return new ValidationResult(
                "Start datum mag niet later of gelijk zijn aan eind datum",
                new[] { nameof(Start), nameof(End) });
        }
    }
}