using Drevenka.Domain.Entities;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IProductService _productService;

    public IndexModel(IProductService productService)
    {
        _productService = productService;
    }

    public int TotalProductsCount { get; private set; }
    public int ActiveProductsCount { get; private set; }
    public decimal AverageMargin { get; private set; }
    public IReadOnlyList<Product> RecentProducts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var all = await _productService.GetAllProductsAsync();
        TotalProductsCount = all.Count;
        ActiveProductsCount = all.Count(p => p.IsActive);

        if (all.Count > 0)
        {
            AverageMargin = all.Average(p => p.SellingPrice - p.PurchasePrice);
            RecentProducts = all.OrderByDescending(p => p.CreatedAt).Take(5).ToList();
        }
    }
}
