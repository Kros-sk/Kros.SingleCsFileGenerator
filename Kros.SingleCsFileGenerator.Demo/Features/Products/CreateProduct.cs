using FluentValidation;
using Kros.SingleCsFileGenerator.Demo.DTOs;
using Kros.SingleCsFileGenerator.Demo.Models;
using Kros.SingleCsFileGenerator.Demo.Repositories;
using Mediator;

namespace Kros.SingleCsFileGenerator.Demo.Features.Products;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity) : IRequest<ProductDto>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateProductHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async ValueTask<ProductDto> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        var created = await repository.AddAsync(product);

        return new ProductDto(
            created.Id,
            created.Name,
            created.Description,
            created.Price,
            created.StockQuantity);
    }
}
