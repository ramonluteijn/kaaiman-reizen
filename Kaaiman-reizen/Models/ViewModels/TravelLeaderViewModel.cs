namespace Kaaiman_reizen.Models.ViewModels;

public sealed class TravelLeaderViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int? AmountOfTrips { get; init; }
    public int? MinTrips { get; init; }
    public int? MaxTrips { get; init; }
    public bool IsActive { get; init; }
    public string Availability { get; init; } = "-";
}
