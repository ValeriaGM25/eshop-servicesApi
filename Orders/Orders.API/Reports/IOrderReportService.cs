namespace Orders.API.Reports;

public interface IOrderReportService
{
    byte[] Generate(Order order);
}
