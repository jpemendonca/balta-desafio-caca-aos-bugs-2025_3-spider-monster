namespace BugStore.Api.Responses.Orders;

public class Get
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalOrderValue { get; set; }
    public List<OrderLineResponse> Lines { get; set; } = new();
}