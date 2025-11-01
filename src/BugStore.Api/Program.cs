using BugStore.Api.Data;
using BugStore.Api.Endpoints.Customer;
using BugStore.Api.Endpoints.Order;
using BugStore.Api.Endpoints.Product;
using BugStore.Api.Handlers.Customers;
using BugStore.Api.Handlers.Order;
using BugStore.Api.Handlers.Product;
using Microsoft.EntityFrameworkCore;
using Handler = BugStore.Api.Handlers.Customers.Handler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(x => x.UseSqlite(connectionString));

builder.Services.AddScoped<ICustomerHandler, Handler>();
builder.Services.AddScoped<IOrderHandle, BugStore.Api.Handlers.Order.Handler>();
builder.Services.AddScoped<IProductHandle, BugStore.Api.Handlers.Product.Handler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

app.MapCustomerEndpoints();
app.MapOrderEndpoints();
app.MapProductEndpoints();

app.Run();