using Drevenka.Domain.Entities;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Products;

public class IndexModel : PageModel
{
    private readonly IProductService _productService;

    public IndexModel(IProductService productService)
    {
        _productService = productService;
    }

    public IReadOnlyList<Product> Products { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool OnlyActive { get; set; } = false;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Products = await _productService.GetAllProductsAsync(SearchTerm, OnlyActive);
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

        return RedirectToPage(new { SearchTerm, OnlyActive });
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

        return RedirectToPage(new { SearchTerm, OnlyActive });
    }
}
