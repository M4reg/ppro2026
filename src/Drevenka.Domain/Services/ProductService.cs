using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;

namespace Drevenka.Domain.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllProductsAsync(string? search = null, bool onlyActive = false, CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task<Product> CreateProductAsync(string code, string name, string? description, decimal purchasePrice, decimal sellingPrice, CancellationToken ct = default);
    Task<Product> UpdateProductAsync(int id, string code, string name, string? description, decimal purchasePrice, decimal sellingPrice, bool isActive, CancellationToken ct = default);
    Task DeleteProductAsync(int id, CancellationToken ct = default);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(string? search = null, bool onlyActive = false, CancellationToken ct = default)
    {
        return await _productRepository.GetAllAsync(search, onlyActive, ct);
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        return await _productRepository.GetByIdAsync(id, ct);
    }

    public async Task<Product> CreateProductAsync(string code, string name, string? description, decimal purchasePrice, decimal sellingPrice, CancellationToken ct = default)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _productRepository.ExistsCodeAsync(normalizedCode, ct: ct))
        {
            throw new DomainException($"Produkt s kódem '{normalizedCode}' již v systému existuje. Kód produktu musí být jedinečný.");
        }

        var product = new Product(normalizedCode, name, description, purchasePrice, sellingPrice);
        await _productRepository.AddAsync(product, ct);
        return product;
    }

    public async Task<Product> UpdateProductAsync(int id, string code, string name, string? description, decimal purchasePrice, decimal sellingPrice, bool isActive, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException($"Produkt s ID {id} nebyl nalezen.");

        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _productRepository.ExistsCodeAsync(normalizedCode, excludeId: id, ct: ct))
        {
            throw new DomainException($"Produkt s kódem '{normalizedCode}' již existuje u jiné položky. Kód produktu musí být jedinečný.");
        }

        product.Update(normalizedCode, name, description, purchasePrice, sellingPrice, isActive);
        await _productRepository.UpdateAsync(product, ct);
        return product;
    }

    public async Task DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException($"Produkt s ID {id} nebyl nalezen.");

        await _productRepository.DeleteAsync(product, ct);
    }
}
