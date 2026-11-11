using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Products;

public class CreateModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public CreateModel(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    [BindProperty]
    public ProductInputModel Input { get; set; } = new();

    public IReadOnlyList<Category> AvailableCategories { get; private set; } = [];

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = [];

    public class ProductInputModel
    {
        [Required(ErrorMessage = "Kód produktu (SKU) je povinný.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Kód produktu musí mít 2 až 50 znaků.")]
        [Display(Name = "Kód produktu (SKU)")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Název produktu je povinný.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Název produktu musí mít 2 až 200 znaků.")]
        [Display(Name = "Název produktu")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Popis nesmí překročit 2000 znaků.")]
        [Display(Name = "Popis položky")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Nákupní cena je povinná.")]
        [Range(0, 1000000, ErrorMessage = "Nákupní cena nesmí být záporná.")]
        [Display(Name = "Nákupní výrobní cena (Kč)")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Prodejní cena je povinná.")]
        [Range(0, 1000000, ErrorMessage = "Prodejní cena nesmí být záporná.")]
        [Display(Name = "Prodejní koncová cena (Kč)")]
        public decimal SellingPrice { get; set; }
    }

    public async Task OnGetAsync()
    {
        AvailableCategories = await _categoryService.GetAllCategoriesAsync(onlyActive: true);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AvailableCategories = await _categoryService.GetAllCategoriesAsync(onlyActive: true);
            return Page();
        }

        try
        {
            var product = await _productService.CreateProductAsync(
                Input.Code,
                Input.Name,
                Input.Description,
                Input.PurchasePrice,
                Input.SellingPrice,
                categoryIds: SelectedCategoryIds
            );

            TempData["SuccessMessage"] = $"Produkt '{product.Name}' (kód {product.Code}) byl úspěšně vytvořen.";
            return RedirectToPage("./Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableCategories = await _categoryService.GetAllCategoriesAsync(onlyActive: true);
            return Page();
        }
    }
}
