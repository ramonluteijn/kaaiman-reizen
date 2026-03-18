using System.ComponentModel.DataAnnotations;

namespace Kaaiman_reizen.Data.Entities;

public class TravelLeader
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefoon is verplicht.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Aantal reizen moet groter zijn dan 0.")]
    public int AmountOfTrips { get; set; }

    public List<PreferredDestination> PreferredDestinations { get; set; } = new();

    public List<AvailabilityPeriod> AvailabilityPeriods { get; set; } = new();
}
