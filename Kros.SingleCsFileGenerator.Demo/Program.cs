using FluentValidation;
using Kros.SingleCsFileGenerator.Demo.DTOs;
using Kros.SingleCsFileGenerator.Demo.Features.Products;
using Kros.SingleCsFileGenerator.Demo.Repositories;
using Mediator;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddMediator(opt =>
{
    opt.Namespace = "Kros.SingleCsFileGenerator.Demo.Mediator";
    opt.ServiceLifetime = ServiceLifetime.Singleton;
    opt.GenerateTypesAsInternal = true;
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

var products = app.MapGroup("/api/products")
    .WithTags("Products");

products.MapGet("/", async (IMediator mediator) =>
{
    var result = await mediator.Send(new GetAllProductsQuery());
    return Results.Ok(result);
})
.WithName("GetAllProducts");

products.MapGet("/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductByIdQuery(id));
    return result is not null
        ? Results.Ok(result)
        : Results.NotFound();
})
.WithName("GetProductById");

products.MapPost("/", async (CreateProductRequest request, IMediator mediator, IValidator<CreateProductCommand> validator) =>
{
    var command = new CreateProductCommand(
        request.Name,
        request.Description,
        request.Price,
        request.StockQuantity);

    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var result = await mediator.Send(command);
    return Results.Created($"/api/products/{result.Id}", result);
})
.WithName("CreateProduct");

app.Run();
