using Kros.SingleCsFileGenerator.Demo.Models;

namespace Kros.SingleCsFileGenerator.Demo.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products =
    [
        new Product
        {
            Id = 1,
            Name = "Laptop",
            Description = "High-performance laptop for developers",
            Price = 1299.99m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        },
        new Product
        {
            Id = 2,
            Name = "Mechanical Keyboard",
            Description = "RGB mechanical keyboard with Cherry MX switches",
            Price = 149.99m,
            StockQuantity = 25,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        },
        new Product
        {
            Id = 3,
            Name = "Monitor",
            Description = "27-inch 4K IPS monitor",
            Price = 449.99m,
            StockQuantity = 15,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        }
    ];

    private int _nextId = 4;

    public Task<IEnumerable<Product>> GetAllAsync()
        => Task.FromResult<IEnumerable<Product>>(_products);

    public Task<Product?> GetByIdAsync(int id)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<Product> AddAsync(Product product)
    {
        product.Id = _nextId++;
        product.CreatedAt = DateTime.UtcNow;
        _products.Add(product);

        return Task.FromResult(product);
    }
}
