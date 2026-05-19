using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Data.Services;

public class JourneyService : IJourneyService
{
    private readonly MainContext _db;
    private readonly IPlanningService _planningService;

    public JourneyService(MainContext db, IPlanningService planningService)
    {
        _db = db;
        _planningService = planningService;
    }

    public async Task<IReadOnlyList<string>> GetLeaderNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.TravelLeader
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Entities.Journey>> GetJourneysAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Journey
            .Include(j => j.TravelLeaders)
            .OrderBy(j => j.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Journey>> GetJourneysWithPublishedPlanningAsync(int year, CancellationToken cancellationToken = default)
    {
        // 1. Haal de reizen op filter on year.
        var allJourneys = await GetJourneysAsync(cancellationToken);

        var journeysForThisYear = allJourneys
            .Where(j => j.Start.Year == year)
            .ToList();

        // 2. Haal de gepubliceerde planning van DIT jaar op
        var publishedPlanning = await _planningService.GetLatestPublishedAsync(year, cancellationToken);

        // 3. Pas de planning toe op alleen de reizen van dit jaar
        return ApplyPlanningVersion(journeysForThisYear, publishedPlanning);
    }

    public async Task AddJourneyAsync(Entities.Journey journey, List<int> selectedLeaders, CancellationToken cancellationToken = default)
    {
        var leaders = await _db.TravelLeader
            .Where(t => selectedLeaders.Contains(t.Id))
            .ToListAsync();

        journey.TravelLeaders = leaders;

        _db.Journey.Add(journey);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteJourneyAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Journey.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
            return;

        _db.Journey.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Entities.Journey?> GetJourneyByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Journey
            .Include(j => j.TravelLeaders)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<Journey?> GetJourneyByIdWithPublishedPlanningAsync(int id, CancellationToken cancellationToken = default)
    {
        var journey = await GetJourneyByIdAsync(id, cancellationToken);
        if (journey is null)
        {
            return null;
        }

        int journeyYear = journey.Start.Year;

        var publishedPlanning = await _planningService.GetLatestPublishedAsync(journeyYear, cancellationToken);

        return ApplyPlanningVersion([journey], publishedPlanning).SingleOrDefault();
    }

    public async Task UpdateJourneyAsync(Entities.Journey journey, List<int> selectedLeaders, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Journey
       .Include(j => j.TravelLeaders)
       .FirstOrDefaultAsync(j => j.Id == journey.Id, cancellationToken);

        if (existing == null)
            return;

        existing.Name = journey.Name;
        existing.Start = journey.Start;
        existing.End = journey.End;
        existing.Busses = journey.Busses;
        existing.Travelers = journey.Travelers;
        existing.BookingStatus = journey.BookingStatus;
        existing.RequiredLeaders = journey.RequiredLeaders;

        var leaders = await _db.TravelLeader
            .Where(t => selectedLeaders.Contains(t.Id))
            .ToListAsync(cancellationToken);

        existing.TravelLeaders.Clear();

        foreach (var leader in leaders)
        {
            existing.TravelLeaders.Add(leader);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> ImportJourneysAsync(Stream stream)
    {
        var workbookPart = SpreadsheetDocument.Open(stream, false).WorkbookPart;
        var sheet = workbookPart!.Workbook.Sheets!.Elements<Sheet>().FirstOrDefault();
        if (sheet == null) return "Geen worksheet gevonden";

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        int rowIndex = 0;

        foreach (var row in sheetData!.Elements<Row>())
        {
            try
            {
                var columns = row.Elements<Cell>().ToList();
                var journey = new Journey
                {
                    Name = sharedStringTable?.ElementAt(int.Parse(columns[0].CellValue?.InnerText ?? string.Empty))?.InnerText ?? string.Empty,
                    Start = DateOnly.FromDateTime(DateTime.FromOADate(double.Parse(columns[1].CellValue?.InnerText ?? string.Empty))),
                    End = DateOnly.FromDateTime(DateTime.FromOADate(double.Parse(columns[2].CellValue?.InnerText ?? string.Empty))),
                    Busses = int.TryParse(columns[3].CellValue?.InnerText ?? string.Empty, out int busses) ? busses : 0,
                    Travelers = int.TryParse(columns[4].CellValue?.InnerText ?? string.Empty, out int travelers) ? travelers : 0,
                    BookingStatus = GetBookingStatus(int.TryParse(columns[5].CellValue?.InnerText ?? string.Empty, out int bookingStatus) ? bookingStatus : 0)
                };

                if (journey.Start >= journey.End) return $"Fout gevonden op rij: {rowIndex}, start datum mag niet later of gelijk zijn aan eind datum. Eerdere rijen zijn wel succesvol toegevoegd";

                await AddJourneyAsync(journey, new List<int>());
            }
            catch
            {
                return $"Fout gevonden op rij: {rowIndex}, eerdere rijen zijn wel succesvol toegevoegd";
            }

            rowIndex++;
        }

        return null;
    }

    private static IReadOnlyList<Journey> ApplyPlanningVersion(
        IReadOnlyList<Journey> journeys,
        PlanningVersion? planningVersion)
    {
        if (planningVersion is null)
        {
            return journeys;
        }

        var assignmentsByJourneyId = planningVersion.Assignments
            .GroupBy(assignment => assignment.JourneyId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(assignment => assignment.TravelLeader)
                    .OrderBy(leader => leader.Name)
                    .ToList());

        foreach (var journey in journeys)
        {
            journey.TravelLeaders = assignmentsByJourneyId.TryGetValue(journey.Id, out var leaders)
                ? leaders
                : [];
        }

        return journeys;
    }

    private BookingStatus GetBookingStatus(int status) => status switch
    {
        0 => BookingStatus.Bezig,
        1 => BookingStatus.Geweest,
        2 => BookingStatus.Geanuleerd,
        _ => 0
    };
}
