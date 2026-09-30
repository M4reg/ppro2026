using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Products;

public class CreateModel : PageModel
{
    private readonly IProductService _productService;

    public CreateModel(IProductService productService)
    {
        _productService = productService;
    }

    [BindProperty]
    public ProductInputModel Input { get; set; } = new();

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
        [Display(Name = "Popis")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Nákupní cena je povinná.")]
        [Range(0, 1000000, ErrorMessage = "Nákupní cena nesmí být záporná.")]
        [Display(Name = "Nákupní cena (Kč)")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Prodejní cena je povinná.")]
        [Range(0, 1000000, ErrorMessage = "Prodejní cena nesmí být záporná.")]
        [Display(Name = "Prodejní cena (Kč)")]
        public decimal SellingPrice { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var product = await _productService.CreateProductAsync(
                Input.Code,
                Input.Name,
                Input.Description,
                Input.PurchasePrice,
                Input.SellingPrice
            );

            TempData["SuccessMessage"] = $"Produkt '{product.Name}' (kód {product.Code}) byl úspěšně vytvořen.";
            return RedirectToPage("./Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
