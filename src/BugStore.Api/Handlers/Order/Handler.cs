using BugStore.Api.Data;
using BugStore.Api.Models;
using Microsoft.EntityFrameworkCore;
// Adicione este using para suas respostas
// Adicione este using para IResult/Results
using Create = BugStore.Api.Requests.Orders.Create;

namespace BugStore.Api.Handlers.Order;

public class Handler(AppDbContext context) : IOrderHandle
{
    public async Task<IResult> GetAllAsync()
    {
        var orders = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .Select(o => new Responses.Orders.Get
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.Name,
                CreatedAt = o.CreatedAt,
                TotalOrderValue = o.Lines.Sum(l => l.Total),
                Lines = o.Lines.Select(l => new Responses.Orders.OrderLineResponse
                {
                    ProductId = l.ProductId,
                    ProductName = l.Product.Title,
                    Quantity = l.Quantity,
                    Total = l.Total
                }).ToList()
            })
            .AsNoTracking()
            .ToListAsync();
        
        return Results.Ok(orders);
    }

    public async Task<IResult> GetByIdAsync(Guid id)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id); // FindAsync não funciona com Include

        if (order is null)
            return Results.NotFound();

        var response = new Responses.Orders.GetById
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer.Name,
            CreatedAt = order.CreatedAt,
            TotalOrderValue = order.Lines.Sum(l => l.Total),
            Lines = order.Lines.Select(l => new Responses.Orders.OrderLineResponse
            {
                ProductId = l.ProductId,
                ProductName = l.Product.Title,
                Quantity = l.Quantity,
                Total = l.Total
            }).ToList()
        };
        
        return Results.Ok(response);
    }

    public async Task<IResult> CreateAsync(Create request)
    {
        var customer = await context.Customers.FindAsync(request.CustomerId);
        if (customer is null)
            return Results.BadRequest(new { Message = "Cliente não encontrado." });

        var productIds = request.Lines.Select(l => l.ProductId).Distinct();
        
        var productsFromDb = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var order = new Models.Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Lines = new List<OrderLine>()
        };

        foreach (var lineRequest in request.Lines)
        {
            if (!productsFromDb.TryGetValue(lineRequest.ProductId, out var product))
                return Results.BadRequest(new { Message = $"Produto {lineRequest.ProductId} não encontrado." });
            
            if (product.Price <= 0) 
                 return Results.BadRequest(new { Message = $"Produto {product.Title} está sem preço." });

            var orderLine = new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = lineRequest.Quantity,
                Total = product.Price * lineRequest.Quantity // Cálculo do total da linha
            };
            order.Lines.Add(orderLine);
        }

        await context.Orders.AddAsync(order); // Adicionar o pedido irá adicionar as linhas (em cascata)
        await context.SaveChangesAsync();
        
        var response = new Responses.Orders.GetById
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = customer.Name,
            CreatedAt = order.CreatedAt,
            TotalOrderValue = order.Lines.Sum(l => l.Total),
            Lines = order.Lines.Select(l => new Responses.Orders.OrderLineResponse
            {
                ProductId = l.ProductId,
                // 'productsFromDb' ainda está acessível no escopo
                ProductName = productsFromDb[l.ProductId].Title, 
                Quantity = l.Quantity,
                Total = l.Total
            }).ToList()
        };
        
        return Results.Created($"/v1/orders/{order.Id}", response);
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null)
            return Results.NotFound();

        context.Orders.Remove(order);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }
}