namespace Kros.SingleCsFileGenerator.Demo.DTOs;

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);
