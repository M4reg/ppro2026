using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Drevenka.Web.Pages.WarehousePages;

public class MovementsModel : PageModel
{
    private readonly IStockService _stockService;
    private readonly IProductService _productService;
    private readonly IWarehouseRepository _warehouseRepository;

    public MovementsModel(
        IStockService stockService,
        IProductService productService,
        IWarehouseRepository warehouseRepository)
    {
        _stockService = stockService;
        _productService = productService;
        _warehouseRepository = warehouseRepository;
    }

    public IReadOnlyList<StockMovement> Movements { get; private set; } = [];
    public List<SelectListItem> ProductOptions { get; private set; } = [];
    public List<SelectListItem> WarehouseOptions { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? WarehouseId { get; set; }

    public async Task OnGetAsync()
    {
        var products = await _productService.GetAllProductsAsync();
        ProductOptions = products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Code} – {p.Name}",
            Selected = ProductId == p.Id
        }).ToList();

        var warehouses = await _warehouseRepository.GetAllAsync(onlyActive: true);
        WarehouseOptions = warehouses.Select(w => new SelectListItem
        {
            Value = w.Id.ToString(),
            Text = w.Name,
            Selected = WarehouseId == w.Id
        }).ToList();

        Movements = await _stockService.GetMovementsHistoryAsync(ProductId, WarehouseId, limit: 150);
    }
}

