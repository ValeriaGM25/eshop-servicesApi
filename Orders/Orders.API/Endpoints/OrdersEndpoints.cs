using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Orders.API.Application.CreateOrder;
using Orders.API.Application.GetOrderById;
using Orders.API.Application.GetOrders;
using Orders.API.Application.GetOrdersByCustomer;
using Orders.API.Application.UpdateOrderStatus;

namespace Orders.API.Endpoints;

public sealed class OrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/", async (
            [FromBody] CreateOrderRequest request,
            HttpRequest httpRequest,
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                throw new BadRequestException("Idempotency-Key header is required.");
            }

            var bearerToken = ExtractBearerToken(httpRequest);
            var customerId = principal.GetAuthenticatedCustomerId();
            var customerName = principal.GetAuthenticatedCustomerName();
            if (!string.IsNullOrWhiteSpace(request.CustomerId) && !string.Equals(request.CustomerId, customerId, StringComparison.Ordinal))
            {
                throw new ForbiddenException("Customers cannot create orders for another user.");
            }

            var result = await sender.Send(new CreateOrderCommand(customerId, customerName, request.BasketId, idempotencyKey, bearerToken), cancellationToken);
            var response = new CreateOrderResponse(result.Order.ToDto(), result.IsReplay);

            return result.IsReplay
                ? Results.Ok(response)
                : Results.Created($"/api/orders/{result.Order.Id}", response);
        })
        .RequireAuthorization("ClienteOnly")
        .WithName("CreateOrder")
        .WithSummary("Create an order from the authenticated customer's basket")
        .WithDescription("Creates an order using Basket.API and Catalog.API. The basketId request field is accepted for exam compatibility, but Basket.API resolves the cart from the forwarded JWT. Replays with the same Idempotency-Key return 200 OK with the original order.")
        .Accepts<CreateOrderRequest>("application/json")
        .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
        .Produces<CreateOrderResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .WithOpenApi(operation =>
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Unique key for idempotent order creation. Reusing the same key returns the original order with 200 OK."
            });
            return operation;
        });

        group.MapGet(string.Empty, async (
            OrderStatus? status,
            string? customerId,
            string? search,
            DateTime? from,
            DateTime? to,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new OrderQueryParameters
            {
                Status = status,
                CustomerId = customerId,
                Search = search,
                From = from,
                To = to,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            };
            var result = await sender.Send(new GetOrdersQuery(query), cancellationToken);
            var response = new PagedResult<OrderSummaryDto>(
                result.Orders.Items.Select(order => order.ToSummaryDto()).ToList(),
                result.Orders.Page,
                result.Orders.PageSize,
                result.Orders.TotalItems,
                result.Orders.TotalPages);

            return Results.Ok(response);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetOrders")
        .WithSummary("List orders for administration")
        .WithDescription("Admin-only endpoint. Supports optional filters: status, customerId, search, from, to, page and pageSize. Search uses existing order fields only: Id, CustomerId, ProductName and ProductId when search is a valid GUID.")
        .Produces<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}", async (
            string id,
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id, principal), cancellationToken);
            return Results.Ok(result.Order.ToDto());
        })
        .RequireAuthorization("ClienteOrAdmin")
        .WithName("GetOrderById")
        .WithSummary("Get order by id")
        .WithDescription("Returns full order details. Cliente can only access own orders; Admin can access any order.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/customer/{customerId}", async (
            string customerId,
            OrderStatus? status,
            string? search,
            DateTime? from,
            DateTime? to,
            int? page,
            int? pageSize,
            ClaimsPrincipal principal,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new OrderQueryParameters
            {
                Status = status,
                Search = search,
                From = from,
                To = to,
                Page = page ?? 1,
                PageSize = pageSize ?? 20
            };
            var result = await sender.Send(new GetOrdersByCustomerQuery(customerId, principal, query), cancellationToken);
            return Results.Ok(result.Orders.Select(order => order.ToDto()).ToList());
        })
        .RequireAuthorization("ClienteOrAdmin")
        .WithName("GetOrdersByCustomer")
        .WithSummary("Get customer orders")
        .WithDescription("Returns orders for a customer. Cliente can only query own customerId; Admin can query any customerId. Optional filters: status, search, from, to, page and pageSize. Response shape remains a list for frontend compatibility.")
        .Produces<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id}/status", async (
            string id,
            [FromBody] UpdateOrderStatusRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UpdateOrderStatusCommand(id, request.Status), cancellationToken);
            return Results.Ok(result.Order.ToDto());
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateOrderStatus")
        .WithSummary("Update order status")
        .WithDescription("Allows Pending -> Confirmed and Pending -> Cancelled. Invalid transitions return 409 Conflict.")
        .Accepts<UpdateOrderStatusRequest>("application/json")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id}/report", async (
            string id,
            ClaimsPrincipal principal,
            ISender sender,
            IOrderReportService reportService,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id, principal), cancellationToken);
            var pdf = reportService.Generate(result.Order);
            return Results.File(pdf, "application/pdf", $"Orden-{result.Order.Id}.pdf");
        })
        .RequireAuthorization("ClienteOrAdmin")
        .WithName("GetOrderReport")
        .WithSummary("Generate order PDF report")
        .WithDescription("Generates the order PDF in the backend and returns application/pdf with a Content-Disposition filename.")
        .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static string ExtractBearerToken(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.FirstOrDefault();
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Bearer token is required.");
        }

        return authorization[prefix.Length..].Trim();
    }
}
