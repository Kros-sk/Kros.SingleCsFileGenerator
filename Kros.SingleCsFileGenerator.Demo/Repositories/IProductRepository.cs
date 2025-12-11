using Kros.SingleCsFileGenerator.Demo.Models;

namespace Kros.SingleCsFileGenerator.Demo.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();

    Task<Product?> GetByIdAsync(int id);

    Task<Product> AddAsync(Product product);
}
