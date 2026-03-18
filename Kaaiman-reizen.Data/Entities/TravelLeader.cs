using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Kaaiman_reizen.Data.Entities;

public class TravelLeader : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefoon is verplicht.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Aantal reizen moet groter of gelijk aan 0 zijn.")]
    public int AmountOfTrips { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Minimaal aantal reizen moet groter of gelijk aan 0 zijn.")]
    public int MinTrips { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Maximaal aantal reizen moet groter of gelijk aan 0 zijn.")]
    public int MaxTrips { get; set; }

    public string Note { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<PreferredDestination> PreferredDestinations { get; set; } = new();

    public List<AvailabilityPeriod> AvailabilityPeriods { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinTrips > MaxTrips)
        {
            yield return new ValidationResult("Minimaal aantal reizen mag niet groter zijn dan maximaal aantal reizen.", new[] { nameof(MinTrips), nameof(MaxTrips) });
        }
    }
}
