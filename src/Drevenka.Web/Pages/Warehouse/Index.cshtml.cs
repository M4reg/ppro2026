using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WarehouseEntity = Drevenka.Domain.Entities.Warehouse;

namespace Drevenka.Web.Pages.WarehousePages;

public class IndexModel : PageModel
{
    private readonly IStockService _stockService;
    private readonly IWarehouseRepository _warehouseRepository;

    public IndexModel(IStockService stockService, IWarehouseRepository warehouseRepository)
    {
        _stockService = stockService;
        _warehouseRepository = warehouseRepository;
    }

    public IReadOnlyList<StockItem> StockItems { get; private set; } = [];
    public IReadOnlyList<WarehouseEntity> Warehouses { get; private set; } = [];
    public List<SelectListItem> WarehouseOptions { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? WarehouseId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool LowStockOnly { get; set; }

    public int TotalStockCount => StockItems.Sum(si => si.QuantityOnHand);
    public int TotalReservedCount => StockItems.Sum(si => si.ReservedQuantity);
    public int TotalAvailableCount => StockItems.Sum(si => si.AvailableQuantity);
    public int LowStockCount => StockItems.Count(si => si.IsLowStock);

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Warehouses = await _warehouseRepository.GetAllAsync(onlyActive: true);

        WarehouseOptions = Warehouses.Select(w => new SelectListItem
        {
            Value = w.Id.ToString(),
            Text = w.Name + (w.IsMain ? " (Centrála)" : ""),
            Selected = WarehouseId == w.Id
        }).ToList();

        StockItems = await _stockService.GetStockOverviewAsync(SearchTerm, WarehouseId, LowStockOnly);
    }
}

