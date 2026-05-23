using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Kaaiman_reizen.Data;
using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Data.Enum;
using Kaaiman_reizen.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Kaaiman_reizen.Tests.Services
{
    public class JourneyServiceTests
    {
        private class DummyServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType) => null;
        }

        private IServiceProvider GetMockServiceProvider()
        {
            return new DummyServiceProvider();
        }

        public static MemoryStream CreateValidJourneyExcel()
        {
            var stream = new MemoryStream();

            using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                var workbookPart = doc.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var sheets = doc.WorkbookPart!.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = doc.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet1"
                });

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

                sheetData.Append(CreateRow(0,
                    "Valid Import Journey", "45000", "45001", "2", "10", "1"));

                workbookPart.Workbook.Save();
            }

            stream.Position = 0;
            return stream;
        }

        private static Row CreateRow(uint index, params string[] values)
        {
            var row = new Row { RowIndex = index };

            for (int i = 0; i < values.Length; i++)
            {
                row.Append(new Cell
                {
                    CellReference = ((char)('A' + i)).ToString() + (index + 1),
                    CellValue = new CellValue(values[i]),
                    DataType = CellValues.String
                });
            }

            return row;
        }

        // Helper: create a fresh in-memory database for each test
        private MainContext GetInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<MainContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new MainContext(options);
        }

        [Fact]
        public async Task AddJourney_Should_Add_Journey()
        {
            var db = GetInMemoryDb();
            var planningService = new PlanningService(db, GetMockServiceProvider());
            var service = new JourneyService(db, planningService);

            var journey = new Journey
            {
                Name = "Test Country",
                Start = new DateOnly(0001, 1, 1),
                End = new DateOnly(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
                BookingStatus = BookingStatus.Bezig,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var allJourneys = await service.GetJourneysAsync();
            Assert.Single(allJourneys);
            Assert.Equal("Test Country", allJourneys.First().Name);
        }

        [Fact]
        public async Task UpdateJourney_Should_Update_Journey()
        {
            var db = GetInMemoryDb();
            var planningService = new PlanningService(db, GetMockServiceProvider());
            var service = new JourneyService(db, planningService);

            var journey = new Journey
            {
                Name = "New Country",
                Start = new DateOnly(0001, 1, 1),
                End = new DateOnly(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
                BookingStatus = BookingStatus.Geweest,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            savedJourney.Name = "New Country";
            await service.UpdateJourneyAsync(savedJourney, new List<int>());

            var updatedJounrey = (await service.GetJourneysAsync()).First();
            Assert.Equal("New Country", updatedJounrey.Name);
        }

        [Fact]
        public async Task DeleteJourney_Should_Remove_Journey()
        {
            var db = GetInMemoryDb();
            var planningService = new PlanningService(db, GetMockServiceProvider());
            var service = new JourneyService(db, planningService);

            var journey = new Journey
            {
                Name = "Delete Country",
                Start = new DateOnly(0001, 1, 1),
                End = new DateOnly(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
                BookingStatus = BookingStatus.Bezig,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            await service.DeleteJourneyAsync(savedJourney.Id);

            var allJourneys = await service.GetJourneysAsync();
            Assert.Empty(allJourneys);
        }

        [Fact]
        public async Task GetJourneyById_Should_Return_Correct_Journey()
        {
            var db = GetInMemoryDb();
            var planningService = new PlanningService(db, GetMockServiceProvider());
            var service = new JourneyService(db, planningService);

            var journey = new Journey
            {
                Name = "Find Country",
                Start = new DateOnly(0001, 1, 1),
                End = new DateOnly(0001, 1, 2),
                Busses = 1,
                Travelers = 1,
                BookingStatus = BookingStatus.Geweest,
            };
            await service.AddJourneyAsync(journey, new List<int>());

            var savedJourney = (await service.GetJourneysAsync()).First();
            var foundJourney = await service.GetJourneyByIdAsync(savedJourney.Id);

            Assert.NotNull(foundJourney);
            Assert.Equal("Find Country", foundJourney.Name);
        }

        [Fact]
        public void Validate_Should_Return_Error_When_StartDate_GreaterThan_EndDate()
        {
            var journey = new Journey
            {
                Name = "Invalid Country",
                Start = new DateOnly(0001, 1, 2),
                End = new DateOnly(0001, 1, 1),
                Busses = 1,
                Travelers = 1,
                BookingStatus = BookingStatus.Bezig,
            };

            var results = journey.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(journey)).ToList();
            Assert.Single(results);
            Assert.Contains("Start datum mag niet later of gelijk zijn aan eind datum", results[0].ErrorMessage);
        }

        [Fact]
        public async Task ImportJourneysAsync_ValidExcel_ReturnsNull()
        {
            var stream = CreateValidJourneyExcel();
            var db = GetInMemoryDb();
            var planningService = new PlanningService(db, GetMockServiceProvider());
            var service = new JourneyService(db, planningService);

            var result = await service.ImportJourneysAsync(stream);

            Assert.Null(result.warning);
        }
    }
}