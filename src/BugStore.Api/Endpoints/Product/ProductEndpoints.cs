using BugStore.Api.Handlers.Product;
using BugStore.Api.Requests.Products;
using Microsoft.AspNetCore.Mvc;

namespace BugStore.Api.Endpoints.Product;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/products");

        group.WithTags("Products");
        
        group.MapGet("/", async (
                [FromServices] IProductHandle handler) =>
            await handler.GetAllAsync());

        group.MapGet("/{id}", async (
                [FromRoute] Guid id,
                [FromServices] IProductHandle handler) =>
            await handler.GetByIdAsync(id));

        group.MapPost("/", async (
                [FromBody] Create request,
                [FromServices] IProductHandle handler) =>
            await handler.CreateAsync(request));

        group.MapDelete("/{id}", async (
                [FromRoute] Guid id,
                [FromServices] IProductHandle handler) =>
            await handler.DeleteAsync(id));
    }
}