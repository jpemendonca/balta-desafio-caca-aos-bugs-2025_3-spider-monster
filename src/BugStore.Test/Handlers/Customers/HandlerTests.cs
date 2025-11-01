using BugStore.Api.Data;
using BugStore.Api.Handlers.Customers;
using BugStore.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BugStore.Test.Handlers.Customers;

public class HandlerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ICustomerHandler _handler;
    private readonly SqliteConnection _connection;

    public HandlerTests()
    {
        // 1. Configurar a conexão com o SQLite in-memory
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // Mantém a conexão aberta para o DB não sumir

        // 2. Configurar as opções do DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // 3. Criar o contexto e o banco
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated(); // Cria o schema do banco

        // 4. Instanciar o Handler CONCRETO com o contexto de teste
        _handler = new BugStore.Api.Handlers.Customers.Handler(_context);
    }
    
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ShouldReturnOkWithCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Cliente Teste",
            Email = "teste@email.com",
            Phone = "9899999999"
        };
        
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.GetByIdAsync(customer.Id);

        // Assert
        var okResult = Assert.IsType<Ok<BugStore.Api.Responses.Customers.GetById>>(result);
        
        Assert.NotNull(okResult.Value);
        Assert.Equal(customer.Name, okResult.Value.Name);
        Assert.Equal(customer.Email, okResult.Value.Email);
    }
    
    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.GetByIdAsync(nonExistentId);

        // Assert
        Assert.IsType<NotFound>(result);
    }
    
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldReturnCreatedAndSaveCustomer()
    {
        // Arrange
        var request = new BugStore.Api.Requests.Customers.Create
        {
            Name = "Novo Cliente",
            Email = "novo@email.com",
            Phone = "123456789"
        };

        // Act
        var result = await _handler.CreateAsync(request);

        // Assert
        var createdResult = Assert.IsType<Created<Customer>>(result);
        
        Assert.NotNull(createdResult.Value);
        Assert.Equal(request.Name, createdResult.Value.Name);

        // 2. Verifica o banco de dados
        var customerInDb = await _context.Customers.FindAsync(createdResult.Value.Id);
        Assert.NotNull(customerInDb);
        Assert.Equal(request.Name, customerInDb.Name);
    }
    
    [Fact]
    public async Task UpdateAsync_WhenCustomerExists_ShouldReturnNoContentAndUpdateCustomer()
    {
        // Arrange
        
        // cria o customer
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Nome Antigo", Email = "antigo@email.com", Phone = "181181818"};
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();
        
        var request = new BugStore.Api.Requests.Customers.Update
        {
            Name = "Nome Novo",
            Email = "novo@email.com",
        };

        // Act
        var result = await _handler.UpdateAsync(customer.Id, request);

        // Assert
        Assert.IsType<NoContent>(result);

        // 2. Verifica o banco de dados
        var updatedCustomer = await _context.Customers.FindAsync(customer.Id);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(request.Name, updatedCustomer.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var request = new BugStore.Api.Requests.Customers.Update()
        {
            Name = "Novo nome",
            Email = "Novo email"
        };
        
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _handler.UpdateAsync(nonExistentId, request);

        // Assert
        Assert.IsType<NotFound>(result);
    }
    
}