using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SocietyGatekeeper.API.Services;

public static class ExportService
{
    public static byte[] ToExcel(string sheetName, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (int i = 0; i < headers.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        int rowIndex = 2;
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Count; i++)
            {
                sheet.Cell(rowIndex, i + 1).Value = row[i]?.ToString() ?? string.Empty;
            }
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] ToPdf(string title, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var rowsList = rows.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Header().Text(title).FontSize(16).Bold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                            header.Cell().Element(HeaderStyle).Text(h);

                        static IContainer HeaderStyle(IContainer c) =>
                            c.DefaultTextStyle(t => t.Bold()).PaddingVertical(4).BorderBottom(1);
                    });

                    foreach (var row in rowsList)
                    {
                        foreach (var cell in row)
                            table.Cell().Element(c => c.PaddingVertical(3).BorderBottom(0.5f)).Text(cell?.ToString() ?? string.Empty);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
