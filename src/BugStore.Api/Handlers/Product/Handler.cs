using System.Text.RegularExpressions;
using BugStore.Api.Data;
using BugStore.Api.Requests.Products;
using Microsoft.EntityFrameworkCore;

namespace BugStore.Api.Handlers.Product;

public class Handler(AppDbContext context) : IProductHandle
{
    public async Task<IResult> GetAllAsync()
    {
        var products = await context.Products
            .Select(p => new Responses.Products.Get
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Price = p.Price
            })
            .AsNoTracking()
            .ToListAsync();
        
        return Results.Ok(products);
    }

    public async Task<IResult> GetByIdAsync(Guid id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
            return Results.NotFound();

        var response = new Responses.Products.GetById
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Slug = product.Slug,
            Price = product.Price
        };
        
        return Results.Ok(response);
    }

    public async Task<IResult> CreateAsync(Create request)
    {
        var product = new Models.Product
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            Slug = GenerateSlug(request.Title)
        };

        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        return Results.Created($"/v1/products/{product.Id}", product);
    }

    public async Task<IResult> UpdateAsync(Guid id, Update request)
    {
        var product = await context.Products.FindAsync(id);
        
        if (product is null)
            return Results.NotFound();

        product.Title = request.Title;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Slug = GenerateSlug(request.Title);

        context.Products.Update(product);
        await context.SaveChangesAsync();
        
        return Results.NoContent();
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
            return Results.NotFound();

        context.Products.Remove(product);
        await context.SaveChangesAsync();

        return Results.NoContent();
    }
    
    private static string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");
        return slug;
    }
}