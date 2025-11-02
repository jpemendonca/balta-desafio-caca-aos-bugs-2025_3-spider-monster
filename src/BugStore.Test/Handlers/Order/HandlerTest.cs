using BugStore.Api.Data;
using BugStore.Api.Handlers.Order;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BugStore.Api.Models; 
using Microsoft.AspNetCore.Http.HttpResults; 
using BugStore.Api.Requests.Orders; 
using BugStore.Api.Responses.Orders;
using Microsoft.AspNetCore.Http;

namespace BugStore.Test.Handlers.Order;

public class HandlerTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IOrderHandle _handler;
    private readonly SqliteConnection _connection;

    public HandlerTest()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _handler = new BugStore.Api.Handlers.Order.Handler(_context);
    }
    
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
    

    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new BugStore.Api.Requests.Orders.Create
        {
            CustomerId = Guid.NewGuid(),
            Lines = new List<LineItemRequest> 
            { 
                new() { ProductId = Guid.NewGuid(), Quantity = 1 } 
            }
        };

        // Act
        var result = await _handler.CreateAsync(request);

        // Assert
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
    
        // 2. Verifique se o status code é 400 (Bad Request)
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_ShouldReturnBadRequest()
    {
        // Arrange
        // 1. Criar um cliente valido
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test Customer", Phone = "992992", Email = "email@EMAIL.com"};
        
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
        
        var request = new BugStore.Api.Requests.Orders.Create
        {
            CustomerId = customer.Id, // ID de cliente VÁLIDO
            Lines = new List<LineItemRequest> 
            { 
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        // Act
        var result = await _handler.CreateAsync(request);
        
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
    
        // 2. Verifique se o status code é 400 (Bad Request)
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldReturnCreatedAndSaveOrder()
    {
        // Arrange
        // 1. Crie um cliente e produto válidos
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test Customer", Email = "email@email.com", Phone = "92222222"};
        
        var product = new Api.Models.Product 
        { 
            Id = Guid.NewGuid(), 
            Title = "Test Product", 
            Price = 10.50m,
            Slug = "New slug",
            Description = "Test Description",
        };
        
        await _context.Customers.AddAsync(customer);
        await _context.Products.AddAsync(product);
        
        await _context.SaveChangesAsync();

        var request = new BugStore.Api.Requests.Orders.Create
        {
            CustomerId = customer.Id,
            Lines = new List<LineItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 2 }
            }
        };

        // Act
        var result = await _handler.CreateAsync(request);

        // Assert
        var createdResult = Assert.IsType<Created<BugStore.Api.Responses.Orders.GetById>>(result);
        
        Assert.NotNull(createdResult.Value);
        Assert.Equal(customer.Id, createdResult.Value.CustomerId);
        Assert.Equal(21.00m, createdResult.Value.TotalOrderValue);
        Assert.Single(createdResult.Value.Lines);

        // 2. Verifica o banco
        var orderInDb = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == createdResult.Value.Id, cancellationToken: TestContext.Current.CancellationToken);
            
        Assert.NotNull(orderInDb);
        Assert.Single(orderInDb.Lines);
        Assert.Equal(21.00m, orderInDb.Lines[0].Total);
    }
    

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.GetByIdAsync(nonExistentId);

        // Assert
        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderExists_ShouldReturnOk()
    {
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test Customer", Email = "email@email.com", Phone = "92222222"};
        
        var product = new Api.Models.Product  
        { 
            Id = Guid.NewGuid(), 
            Title = "Test Product", 
            Price = 10.50m,
            Slug = "New slug",
            Description = "Test Description",
        };        
        
        var order = new Api.Models.Order
        {
            Id = Guid.NewGuid(),
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<OrderLine>
            {
                new() { Id = Guid.NewGuid(), Product = product, Quantity = 2, Total = 10 }
            }
        };
        
        await _context.Customers.AddAsync(customer);
        await _context.Products.AddAsync(product);
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.GetByIdAsync(order.Id);

        // Assert
        var okResult = Assert.IsType<Ok<BugStore.Api.Responses.Orders.GetById>>(result);
        
        Assert.NotNull(okResult.Value);
        Assert.Equal(order.Id, okResult.Value.Id);
        
        Assert.Equal(customer.Name, okResult.Value.CustomerName);
        Assert.Equal(product.Title, okResult.Value.Lines[0].ProductName);
        Assert.Equal(10, okResult.Value.TotalOrderValue);
    }

    
    [Fact]
    public async Task DeleteAsync_WhenOrderExists_ShouldReturnNoContentAndRemoveOrder()
    {
        // Arrange
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test Customer", Email = "email@email.com", Phone = "92222222"};
        
        var product = new Api.Models.Product  
        { 
            Id = Guid.NewGuid(), 
            Title = "Test Product", 
            Price = 10.50m,
            Slug = "New slug",
            Description = "Test Description",
        };        
        
        var order = new Api.Models.Order
        {
            Id = Guid.NewGuid(),
            Customer = customer,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<OrderLine>
            {
                new() { Id = Guid.NewGuid(), Product = product, Quantity = 2, Total = 10 }
            }
        };
        
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.DeleteAsync(order.Id);

        // Assert
        Assert.IsType<NoContent>(result);
        var deletedOrder = await _context.Orders.FindAsync(order.Id);
        Assert.Null(deletedOrder);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrderDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.DeleteAsync(nonExistentId);

        // Assert
        Assert.IsType<NotFound>(result);
    }
}