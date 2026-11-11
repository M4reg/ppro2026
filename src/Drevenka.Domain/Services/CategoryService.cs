using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;

namespace Drevenka.Domain.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(bool onlyActive = false, CancellationToken ct = default);
    Task<Category?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task<Category> CreateCategoryAsync(string name, string? description = null, CancellationToken ct = default);
    Task<Category> UpdateCategoryAsync(int id, string name, string? description, bool isActive, CancellationToken ct = default);
    Task DeleteCategoryAsync(int id, CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(bool onlyActive = false, CancellationToken ct = default)
    {
        return await _categoryRepository.GetAllAsync(onlyActive, ct);
    }

    public async Task<Category?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        return await _categoryRepository.GetByIdAsync(id, ct);
    }

    public async Task<Category> CreateCategoryAsync(string name, string? description = null, CancellationToken ct = default)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (await _categoryRepository.ExistsNameAsync(trimmedName, ct: ct))
        {
            throw new DomainException($"Kategorie s názvem '{trimmedName}' již v systému existuje. Název kategorie musí být jedinečný.");
        }

        var category = new Category(trimmedName, description);
        await _categoryRepository.AddAsync(category, ct);
        return category;
    }

    public async Task<Category> UpdateCategoryAsync(int id, string name, string? description, bool isActive, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException($"Kategorie s ID {id} nebyla nalezena.");

        var trimmedName = name?.Trim() ?? string.Empty;
        if (await _categoryRepository.ExistsNameAsync(trimmedName, excludeId: id, ct: ct))
        {
            throw new DomainException($"Kategorie s názvem '{trimmedName}' již existuje u jiné položky. Název kategorie musí být jedinečný.");
        }

        category.Update(trimmedName, description, isActive);
        await _categoryRepository.UpdateAsync(category, ct);
        return category;
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, ct)
            ?? throw new DomainException($"Kategorie s ID {id} nebyla nalezena.");

        await _categoryRepository.DeleteAsync(category, ct);
    }
}

