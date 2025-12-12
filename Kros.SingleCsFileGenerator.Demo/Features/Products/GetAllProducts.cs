using Kros.SingleCsFileGenerator.Demo.DTOs;
using Kros.SingleCsFileGenerator.Demo.Repositories;
using Mediator;

namespace Kros.SingleCsFileGenerator.Demo.Features.Products;

public record GetAllProductsQuery : IRequest<IEnumerable<ProductDto>>;

public class GetAllProductsHandler(IProductRepository repository)
    : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    public async ValueTask<IEnumerable<ProductDto>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync();

        return products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.StockQuantity));
    }
}
