using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SocietyGatekeeper.Domain.Entities;

namespace SocietyGatekeeper.API.Services;

public static class InvoicePdfGenerator
{
    private static readonly string PrimaryColor = "#0E5E4C";
    private static readonly string MutedColor = "#5C6B64";
    private static readonly string LineColor = "#DCE5DF";
    private static readonly string SurfaceColor = "#F7FAF8";

    public static byte[] Generate(Invoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#17221E"));

                page.Header().Element(c => ComposeHeader(c, invoice));
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().Element(c => ComposeFooter(c, invoice));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Row(logoRow =>
                {
                    logoRow.ConstantItem(40).Height(40).Background(PrimaryColor).AlignCenter().AlignMiddle()
                        .Text(GetInitials(invoice.SocietyName)).FontColor(Colors.White).FontSize(16).Bold();

                    logoRow.RelativeItem().PaddingLeft(10).Column(nameCol =>
                    {
                        nameCol.Item().Text(invoice.SocietyName).FontSize(16).Bold().FontColor(PrimaryColor);
                        nameCol.Item().Text("Maintenance Payment Invoice").FontSize(9).FontColor(MutedColor);
                    });
                });
            });

            row.ConstantItem(170).Column(col =>
            {
                col.Item().AlignRight().Text("PAID").FontSize(22).Bold().FontColor("#1E7A46");
                col.Item().AlignRight().Text($"Invoice #{invoice.InvoiceNumber}").FontSize(9).FontColor(MutedColor);
                col.Item().AlignRight().Text($"Generated {invoice.CreatedAt:dd MMM yyyy, hh:mm tt}").FontSize(8).FontColor(MutedColor);
            });
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(16);

            // Bill-to / payment summary strip
            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => InfoBlock(c, "BILLED TO", new[]
                {
                    invoice.OwnerName,
                    $"Flat {invoice.FlatNumber}, {invoice.WingName}, {invoice.BlockName}",
                    invoice.OwnerPhone ?? "-",
                    invoice.OwnerEmail ?? "-"
                }));

                row.RelativeItem().Element(c => InfoBlock(c, "PAYMENT DETAILS", new[]
                {
                    $"Date: {invoice.PaymentDateTime:dd MMM yyyy, hh:mm tt}",
                    $"Mode: {invoice.PaymentMode}",
                    $"Reference: {invoice.TransactionReference ?? "-"}",
                    $"Status: PAID"
                }));
            });

            // Amount table
            column.Item().Border(1).BorderColor(LineColor).Padding(16).Column(tableCol =>
            {
                tableCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().Text("Description").Bold();
                    table.Cell().AlignRight().Text("Amount").Bold();

                    table.Cell().PaddingTop(6).Text($"Maintenance charges — {invoice.BillingPeriod}");
                    table.Cell().PaddingTop(6).AlignRight().Text($"{invoice.AmountPaid:N2}");
                });

                tableCol.Item().PaddingTop(10).BorderTop(1).BorderColor(LineColor).PaddingTop(10).Row(totalRow =>
                {
                    totalRow.RelativeItem().Text("Total Paid").Bold().FontSize(13);
                    totalRow.ConstantItem(140).AlignRight().Text($"INR {invoice.AmountPaid:N2}").Bold().FontSize(13).FontColor(PrimaryColor);
                });
            });

            column.Item().Background(SurfaceColor).Padding(10).Text(
                "This is a computer-generated invoice confirming a successful maintenance payment. " +
                "No physical signature is required.").FontSize(8).FontColor(MutedColor).Italic();
        });
    }

    private static void InfoBlock(IContainer container, string title, string[] lines)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(8).Bold().FontColor(MutedColor);
            foreach (var line in lines)
            {
                col.Item().PaddingTop(2).Text(line).FontSize(10);
            }
        });
    }

    private static void ComposeFooter(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Item().BorderTop(1).BorderColor(LineColor).PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(footCol =>
                {
                    footCol.Item().Text($"{invoice.SocietyName}").FontSize(8).Bold();
                    footCol.Item().Text("Authorized Society Management — this receipt is system-generated and does not require a physical signature.")
                        .FontSize(7).FontColor(MutedColor);
                });

                row.ConstantItem(120).AlignRight().Column(pageCol =>
                {
                    pageCol.Item().AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontSize(7).FontColor(MutedColor);
                        x.CurrentPageNumber().FontSize(7).FontColor(MutedColor);
                        x.Span(" of ").FontSize(7).FontColor(MutedColor);
                        x.TotalPages().FontSize(7).FontColor(MutedColor);
                    });
                });
            });
        });
    }

    private static string GetInitials(string societyName)
    {
        var parts = societyName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        return initials.Length > 0 ? initials : "S";
    }
}
