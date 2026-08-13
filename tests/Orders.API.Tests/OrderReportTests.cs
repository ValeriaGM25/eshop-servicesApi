using BuildingBlocks;
using Orders.API.Application.GetOrderById;
using Orders.API.Domain;
using Orders.API.Reports;
using System.Security.Claims;

namespace Orders.API.Tests;

public sealed class OrderReportTests
{
    [Fact]
    public void Existing_order_report_generates_pdf_bytes()
    {
        var order = SampleOrder();
        var service = new QuestPdfOrderReportService();

        var pdf = service.Generate(order);

        Assert.True(pdf.Length > 4);
        Assert.Equal('%', (char)pdf[0]);
        Assert.Equal('P', (char)pdf[1]);
        Assert.Equal('D', (char)pdf[2]);
        Assert.Equal('F', (char)pdf[3]);
    }

    [Fact]
    public void Order_report_contains_order_customer_items_and_totals()
    {
        var order = SampleOrder();

        var reportData = QuestPdfOrderReportService.CreateReportData(order, new DateTime(2026, 8, 13, 15, 0, 0, DateTimeKind.Utc));

        Assert.Equal(order.Id, reportData.OrderId);
        Assert.Equal("Valeria Galindo Marin", reportData.CustomerName);
        Assert.Equal(order.CustomerId, reportData.CustomerId);
        Assert.Equal("2026-08-13 14:30:00 UTC", reportData.CreatedAtText);
        Assert.Equal(order.Status.ToString(), reportData.Status);
        Assert.Equal("Keyboard", reportData.Items[0].ProductName);
        Assert.Equal("1", reportData.Items[0].Quantity);
        Assert.Equal("$10.00", reportData.Items[0].UnitPrice);
        Assert.Equal("$10.00", reportData.Items[0].LineTotal);
        Assert.Equal("$10.00", reportData.Subtotal);
        Assert.Equal("$1.80", reportData.Tax);
        Assert.Equal("$11.80", reportData.Total);
    }

    [Fact]
    public void Old_order_without_customer_name_generates_pdf_with_professional_fallback()
    {
        var order = SampleOrder();
        order.CustomerName = null;
        var service = new QuestPdfOrderReportService();

        var pdf = service.Generate(order);
        var reportData = QuestPdfOrderReportService.CreateReportData(order, DateTime.UtcNow);

        Assert.True(pdf.Length > 4);
        Assert.Equal("No disponible", reportData.CustomerName);
        Assert.Equal(order.CustomerId, reportData.CustomerId);
    }

    [Fact]
    public async Task Admin_report_uses_persisted_buyer_name_not_admin_name()
    {
        var repository = new InMemoryOrdersRepository();
        var order = SampleOrder();
        repository.Add(order);
        var handler = new GetOrderByIdQueryHandler(repository);
        var service = new QuestPdfOrderReportService();

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id, Principal("admin-1", "Admin", "Administrador")), CancellationToken.None);
        var pdf = service.Generate(result.Order);
        var reportData = QuestPdfOrderReportService.CreateReportData(result.Order, DateTime.UtcNow);

        Assert.True(pdf.Length > 4);
        Assert.Equal("Valeria Galindo Marin", reportData.CustomerName);
        Assert.NotEqual("Administrador", reportData.CustomerName);
    }

    [Fact]
    public async Task Missing_order_for_report_returns_not_found_from_query()
    {
        var handler = new GetOrderByIdQueryHandler(new InMemoryOrdersRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetOrderByIdQuery("missing", Principal()), CancellationToken.None));
    }

    private static Order SampleOrder()
    {
        return new Order
        {
            Id = "68bd3a7a89aa34e8903972a1",
            CustomerId = "customer-1",
            CustomerName = "Valeria Galindo Marin",
            CreatedAt = new DateTime(2026, 8, 13, 14, 30, 0, DateTimeKind.Utc),
            Status = OrderStatus.Pending,
            IdempotencyKey = "report-key",
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Keyboard",
                    Quantity = 1,
                    UnitPrice = 10m,
                    LineTotal = 10m
                }
            ],
            Subtotal = 10m,
            Tax = 1.8m,
            Total = 11.8m
        };
    }

    private static ClaimsPrincipal Principal()
    {
        return Principal("customer-1", "Cliente", "Valeria Galindo Marin");
    }

    private static ClaimsPrincipal Principal(string customerId, string role, string name)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, customerId),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));
    }

}
