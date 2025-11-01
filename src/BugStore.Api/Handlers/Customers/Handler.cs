using BugStore.Api.Data;
using BugStore.Api.Models;
using BugStore.Api.Requests.Customers;
using Microsoft.EntityFrameworkCore;

namespace BugStore.Api.Handlers.Customers;

public class Handler(AppDbContext context) : ICustomerHandler
{
    public async Task<IResult> GetAllAsync()
    {
        var customers = await context.Customers
            .Select(c => new Responses.Customers.Get 
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                BirthDate = c.BirthDate,
                Phone = c.Phone
            })
            .ToListAsync();
        
        return Results.Ok(customers);
    }
    
    public async Task<IResult> GetByIdAsync(Guid id)
    {
        var customer = await context.Customers.FindAsync(id);
        
        if (customer is null)
            return Results.NotFound();
        
        var response = new Responses.Customers.GetById
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            BirthDate = customer.BirthDate,
            Phone = customer.Phone
        };
        
        return Results.Ok(response);
    }

    public async Task<IResult> CreateAsync(Create request)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            BirthDate = request.BirthDate
        };

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        return Results.Created($"/v1/customers/{customer.Id}", customer);
    }

    public async Task<IResult> UpdateAsync(Guid id, Requests.Customers.Update request)
    {
        var customer = await context.Customers.FindAsync(id);
        
        if (customer is null)
            return Results.NotFound();

        customer.Name = request.Name;
        customer.Email = request.Email;

        context.Customers.Update(customer);
        await context.SaveChangesAsync();
        
        return Results.NoContent();
    }
    
    public async Task<IResult> DeleteAsync(Guid id)
    {
        var customer = await context.Customers.FindAsync(id);
        if (customer is null)
            return Results.NotFound();

        context.Customers.Remove(customer);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }
}