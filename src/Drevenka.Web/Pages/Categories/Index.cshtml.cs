using Drevenka.Domain.Entities;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public IndexModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public IReadOnlyList<Category> Categories { get; private set; } = [];

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetAllCategoriesAsync();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        try
        {
            var cat = await _categoryService.GetCategoryByIdAsync(id);
            if (cat != null)
            {
                await _categoryService.UpdateCategoryAsync(cat.Id, cat.Name, cat.Description, isActive: false);
                SuccessMessage = $"Kategorie '{cat.Name}' byla deaktivována.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při deaktivaci kategorie: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        try
        {
            var cat = await _categoryService.GetCategoryByIdAsync(id);
            if (cat != null)
            {
                await _categoryService.UpdateCategoryAsync(cat.Id, cat.Name, cat.Description, isActive: true);
                SuccessMessage = $"Kategorie '{cat.Name}' byla aktivována.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při aktivaci kategorie: {ex.Message}";
        }

        return RedirectToPage();
    }
}

