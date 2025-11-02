using BugStore.Api.Data;
using BugStore.Api.Handlers.Product;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BugStore.Test.Handlers.Product;

public class HandlerTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IProductHandle _handler;
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

        _handler = new BugStore.Api.Handlers.Product.Handler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
    
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldReturnCreatedAndSaveProduct()
    {
        // Arrange
        var request = new Api.Requests.Products.Create
        {
            Title = "Novo Produto",
            Description = "Descricao",
            Price = 19.99m
        };

        // Act
        var result = await _handler.CreateAsync(request);

        // Assert
        var createdResult = Assert.IsType<Created<Api.Models.Product>>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(request.Title, createdResult.Value.Title);
        Assert.Equal("novo-produto", createdResult.Value.Slug);

        // 2. Verifica o banco de dados
        var productInDb = await _context.Products.FindAsync(createdResult.Value.Id);
        Assert.NotNull(productInDb);
        Assert.Equal(request.Title, productInDb.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.GetByIdAsync(nonExistentId);

        // Assert
        // Verifica se o IResult é do tipo "NotFound"
        Assert.IsType<NotFound>(result);
    }
}