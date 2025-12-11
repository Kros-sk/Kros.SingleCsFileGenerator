using Kros.SingleCsFileGenerator.Demo.DTOs;
using Kros.SingleCsFileGenerator.Demo.Repositories;
using MediatR;

namespace Kros.SingleCsFileGenerator.Demo.Features.Products;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;

public class GetProductByIdHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id);

        if (product is null)
        {
            return null;
        }

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity);
    }
}
