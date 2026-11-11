using Drevenka.Domain.Entities;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Drevenka.Web.Pages.Products;

public class IndexModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public IndexModel(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public IReadOnlyList<Product> Products { get; private set; } = [];
    public List<SelectListItem> CategoryOptions { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyActive { get; set; } = false;

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name_asc";

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        CategoryId.HasValue ||
        StatusFilter != "all" ||
        OnlyActive ||
        SortBy != "name_asc" ||
        MinPrice.HasValue ||
        MaxPrice.HasValue;

    public async Task OnGetAsync()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        CategoryOptions = categories.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = CategoryId == c.Id
        }).ToList();

        bool fetchOnlyActive = StatusFilter == "active" || (StatusFilter != "inactive" && OnlyActive);
        var list = (await _productService.GetAllProductsAsync(SearchTerm, CategoryId, fetchOnlyActive)).ToList();

        if (StatusFilter == "inactive")
        {
            list = list.Where(p => !p.IsActive).ToList();
        }

        if (MinPrice.HasValue)
        {
            list = list.Where(p => p.SellingPrice >= MinPrice.Value).ToList();
        }

        if (MaxPrice.HasValue)
        {
            list = list.Where(p => p.SellingPrice <= MaxPrice.Value).ToList();
        }

        list = SortBy switch
        {
            "price_asc" => list.OrderBy(p => p.SellingPrice).ToList(),
            "price_desc" => list.OrderByDescending(p => p.SellingPrice).ToList(),
            "margin_desc" => list.OrderByDescending(p => p.SellingPrice - p.PurchasePrice).ToList(),
            "code_asc" => list.OrderBy(p => p.Code).ToList(),
            "name_desc" => list.OrderByDescending(p => p.Name).ToList(),
            _ => list.OrderBy(p => p.Name).ToList()
        };

        Products = list;
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null)
            {
                await _productService.UpdateProductAsync(
                    product.Id,
                    product.Code,
                    product.Name,
                    product.Description,
                    product.PurchasePrice,
                    product.SellingPrice,
                    isActive: false
                );
                SuccessMessage = $"Produkt '{product.Name}' byl deaktivován.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při deaktivaci produktu: {ex.Message}";
        }

        return RedirectToPage(new { SearchTerm, CategoryId, OnlyActive });
    }

    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null)
            {
                await _productService.UpdateProductAsync(
                    product.Id,
                    product.Code,
                    product.Name,
                    product.Description,
                    product.PurchasePrice,
                    product.SellingPrice,
                    isActive: true
                );
                SuccessMessage = $"Produkt '{product.Name}' byl aktivován.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Chyba při aktivaci produktu: {ex.Message}";
        }

        return RedirectToPage(new { SearchTerm, CategoryId, OnlyActive });
    }
}
