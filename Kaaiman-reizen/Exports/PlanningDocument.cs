using Kaaiman_reizen.Data.Entities;
using Kaaiman_reizen.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kaaiman_reizen.Exports
{
    public class PlanningDocument : IDocument
    {
        private readonly List<Journey> _reizen;
        private readonly string _printedOn;

        public PlanningDocument(List<Journey> reizen, string? printedOn = null)
        {
            _reizen = reizen;
            _printedOn = string.IsNullOrWhiteSpace(printedOn)
                ? DateDisplay.FormatDate(DateTime.UtcNow)
                : printedOn;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.Size(PageSizes.A4);

                page.Header().Column(col =>
                {
                    col.Item().PaddingBottom(15).MaxHeight(50).Image("wwwroot/images/Kaaiman-reizen-logo.webp");
                    col.Item().PaddingBottom(10).Text("Planning").FontSize(24).SemiBold().FontColor(Colors.Black);
                    col.Item().PaddingBottom(20).Text("Geprint op: " + _printedOn).FontSize(14).Italic();
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(20).Text("Overzicht Reizen").FontSize(18).SemiBold();

                    foreach (var reis in _reizen)
                    {
                        col.Item().Element(container => RenderReis(container, reis));
                        col.Item().PaddingVertical(10);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ");
                    x.CurrentPageNumber();
                    x.Span(" van ");
                    x.TotalPages();
                });
            });
        }

        void RenderReis(IContainer container, Journey reis)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"{reis.Name}").FontSize(16).SemiBold();
                    row.RelativeItem().AlignRight().Text($"{DateDisplay.FormatDate(reis.Start)} - {DateDisplay.FormatDate(reis.End)}");
                });

                col.Item().PaddingTop(10).PaddingBottom(5).Text("Reisleiders:").SemiBold();

                col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Naam");
                        header.Cell().Element(HeaderStyle).Text("Telefoon");

                        static IContainer HeaderStyle(IContainer container) =>
                            container.Background(Colors.Grey.Lighten4)
                             .BorderBottom(1)
                             .BorderColor(Colors.Grey.Lighten3)
                             .PaddingVertical(5)
                             .PaddingHorizontal(5)
                             .DefaultTextStyle(x => x.Bold());
                    });

                    int i = 0;
                    foreach (var reisleider in reis.TravelLeaders)
                    {
                        var backgroundColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        table.Cell().Element(CellStyle).Text(reisleider.Name);
                        table.Cell().Element(CellStyle).Text(reisleider.PhoneNumber);

                        IContainer CellStyle(IContainer container)
                        {
                            return container.Background(backgroundColor)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(10)
                                    .PaddingHorizontal(5);
                        }
                        i++;
                    }
                });

                col.Item().PaddingBottom(7);
            });
        }
    }
}