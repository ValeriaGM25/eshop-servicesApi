using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Orders.API.Reports;

public sealed class QuestPdfOrderReportService : IOrderReportService
{
    private const string Violet = "6D28D9";
    private const string Cyan = "06B6D4";
    private const string Ink = "111827";
    private const string Muted = "6B7280";
    private const string Surface = "F8FAFC";
    private const string Border = "E5E7EB";

    public byte[] Generate(Order order)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var generatedAt = DateTime.UtcNow;
        var reportData = CreateReportData(order, generatedAt);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Ink));

                page.Header().Element(header => ComposeHeader(header, order));
                page.Content().PaddingVertical(22).Column(column =>
                {
                    column.Spacing(16);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(card => ComposeCustomerCard(card, reportData.CustomerName, reportData.CustomerId));
                        row.ConstantItem(18);
                        row.RelativeItem().Element(card => ComposeOrderInfoCard(card, reportData));
                    });

                    column.Item().Element(content => ComposeItemsTable(content, order));
                    column.Item().Element(content => ComposePaymentSummary(content, order));
                });

                page.Footer().Element(footer => ComposeFooter(footer, reportData.GeneratedAtText));
            });
        }).GeneratePdf();
    }

    public static OrderReportData CreateReportData(Order order, DateTime generatedAt)
    {
        return new OrderReportData(
            order.Id,
            order.GetCustomerNameDisplay(),
            order.CustomerId,
            $"{order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC",
            order.Status.ToString(),
            order.Items.Sum(item => item.Quantity),
            order.Items.Select(item => new OrderReportItemData(
                item.ProductName,
                item.Quantity.ToString(),
                FormatCurrency(item.UnitPrice),
                FormatCurrency(item.LineTotal))).ToList(),
            FormatCurrency(order.Subtotal),
            FormatCurrency(order.Tax),
            FormatCurrency(order.Total),
            $"{generatedAt:yyyy-MM-dd HH:mm:ss} UTC");
    }

    private static void ComposeHeader(IContainer container, Order order)
    {
        container.Column(column =>
        {
            column.Item().Background(Surface).Border(1).BorderColor(Border).CornerRadius(12).Padding(18).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("E-Shop Neo").FontSize(24).Bold().FontColor(Violet);
                    left.Item().PaddingTop(3).Text("Comprobante de compra").FontSize(12).FontColor(Muted);
                    left.Item().PaddingTop(10).Height(3).Width(120).Background(Cyan);
                });

                row.ConstantItem(220).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text("ORDEN").FontSize(9).SemiBold().FontColor(Muted);
                    right.Item().AlignRight().Text(order.Id).FontSize(11).Bold().FontColor(Ink);
                    right.Item().PaddingTop(10).AlignRight().Element(badge => ComposeStatusBadge(badge, order.Status));
                });
            });
        });
    }

    private static void ComposeCustomerCard(IContainer container, string customerName, string customerId)
    {
        ComposeCard(container, "Cliente", card =>
        {
            card.Item().Element(content => ComposeLabelValue(content, "Nombre", customerName));
            card.Item().PaddingTop(10).Element(content => ComposeLabelValue(content, "Identificador", customerId));
        });
    }

    private static void ComposeOrderInfoCard(IContainer container, OrderReportData reportData)
    {
        ComposeCard(container, "Informacion de la orden", card =>
        {
            card.Item().Element(content => ComposeLabelValue(content, "Fecha de compra", reportData.CreatedAtText));
            card.Item().PaddingTop(10).Element(content => ComposeLabelValue(content, "Estado", reportData.Status));
            card.Item().PaddingTop(10).Element(content => ComposeLabelValue(content, "Cantidad de productos", reportData.ItemsCount.ToString()));
        });
    }

    private static void ComposeItemsTable(IContainer container, Order order)
    {
        container.Column(column =>
        {
            column.Item().Text("Detalle de productos").FontSize(14).Bold().FontColor(Violet);
            column.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(95);
                    columns.ConstantColumn(95);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Producto");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cantidad");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Precio unitario");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Importe");
                });

                foreach (var item in order.Items)
                {
                    table.Cell().Element(BodyCell).Text(item.ProductName);
                    table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(item.UnitPrice));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(item.LineTotal));
                }
            });
        });
    }

    private static void ComposePaymentSummary(IContainer container, Order order)
    {
        container.AlignRight().Width(260).Column(column =>
        {
            column.Spacing(8);
            column.Item().Element(content => ComposeAmountRow(content, "Subtotal", order.Subtotal, false));
            column.Item().Element(content => ComposeAmountRow(content, "Impuestos", order.Tax, false));
            column.Item().LineHorizontal(1).LineColor(Border);
            column.Item().Background(Surface).Border(1).BorderColor(Border).CornerRadius(8).Padding(10)
                .Element(content => ComposeAmountRow(content, "TOTAL", order.Total, true));
        });
    }

    private static void ComposeFooter(IContainer container, string generatedAtText)
    {
        container.BorderTop(1).BorderColor(Border).PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text($"E-Shop Neo · Comprobante generado por Orders.API · {generatedAtText}")
                .FontSize(8)
                .FontColor(Muted);
            row.ConstantItem(90).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                text.Span("Pagina ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private static void ComposeCard(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Border(1).BorderColor(Border).CornerRadius(10).Padding(14).Column(column =>
        {
            column.Item().Text(title).FontSize(13).Bold().FontColor(Violet);
            column.Item().PaddingTop(10).Column(content);
        });
    }

    private static void ComposeLabelValue(IContainer container, string label, string value)
    {
        container.Column(column =>
        {
            column.Item().Text(label).FontSize(8).SemiBold().FontColor(Muted);
            column.Item().PaddingTop(2).Text(value).FontSize(10).FontColor(Ink);
        });
    }

    private static void ComposeAmountRow(IContainer container, string label, decimal amount, bool total)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(total ? 12 : 10).Bold().FontColor(total ? Violet : Ink);
            row.ConstantItem(110).AlignRight().Text(FormatCurrency(amount)).FontSize(total ? 13 : 10).Bold().FontColor(total ? Violet : Ink);
        });
    }

    private static void ComposeStatusBadge(IContainer container, OrderStatus status)
    {
        var label = status switch
        {
            OrderStatus.Pending => "Pendiente",
            OrderStatus.Confirmed => "Confirmada",
            OrderStatus.Cancelled => "Cancelada",
            _ => status.ToString()
        };

        var color = status switch
        {
            OrderStatus.Pending => Cyan,
            OrderStatus.Confirmed => "16A34A",
            OrderStatus.Cancelled => "DC2626",
            _ => Muted
        };

        container.Background(color).CornerRadius(8).PaddingHorizontal(12).PaddingVertical(6).Text(label).FontColor(Colors.White).SemiBold();
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container.Background(Violet).PaddingVertical(8).PaddingHorizontal(8).DefaultTextStyle(style => style.FontColor(Colors.White).SemiBold());
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container.BorderBottom(0.5f).BorderColor(Border).PaddingVertical(9).PaddingHorizontal(8);
    }

    private static string FormatCurrency(decimal value)
    {
        return $"${value:0.00}";
    }
}

public sealed record OrderReportData(
    string OrderId,
    string CustomerName,
    string CustomerId,
    string CreatedAtText,
    string Status,
    int ItemsCount,
    IReadOnlyList<OrderReportItemData> Items,
    string Subtotal,
    string Tax,
    string Total,
    string GeneratedAtText);

public sealed record OrderReportItemData(
    string ProductName,
    string Quantity,
    string UnitPrice,
    string LineTotal);
