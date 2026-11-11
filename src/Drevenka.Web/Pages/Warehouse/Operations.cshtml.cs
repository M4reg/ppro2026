using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Drevenka.Web.Pages.WarehousePages;

public class OperationsModel : PageModel
{
    private readonly IStockService _stockService;
    private readonly IProductService _productService;
    private readonly IWarehouseRepository _warehouseRepository;

    public OperationsModel(
        IStockService stockService,
        IProductService productService,
        IWarehouseRepository warehouseRepository)
    {
        _stockService = stockService;
        _productService = productService;
        _warehouseRepository = warehouseRepository;
    }

    public List<SelectListItem> ProductOptions { get; private set; } = [];
    public List<SelectListItem> WarehouseOptions { get; private set; } = [];

    [BindProperty]
    public OperationInputModel Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class OperationInputModel
    {
        [Required(ErrorMessage = "Zvolte typ operace.")]
        public string OperationType { get; set; } = "receive"; // receive, issue, transfer, adjust

        [Required(ErrorMessage = "Zvolte produkt.")]
        [Display(Name = "Produkt")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Zvolte sklad.")]
        [Display(Name = "Sklad")]
        public int WarehouseId { get; set; }

        [Display(Name = "Cílový sklad (pro převoz)")]
        public int? TargetWarehouseId { get; set; }

        [Required(ErrorMessage = "Zadejte množství.")]
        [Range(1, 100000, ErrorMessage = "Množství musí být alespoň 1 ks.")]
        [Display(Name = "Množství (ks)")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Skutečný počet kusů (pro inventuru)")]
        public int? ActualCount { get; set; }

        [Display(Name = "Číslo dokladu / objednávky")]
        public string? ReferenceNumber { get; set; }

        [Display(Name = "Poznámka / Odůvodnění")]
        public string? Note { get; set; }

        [Display(Name = "Osoba provádějící")]
        public string PerformedBy { get; set; } = "Petr Doležal";
    }

    public async Task OnGetAsync(int? productId = null, int? warehouseId = null, string? op = null)
    {
        await LoadDropdownsAsync();

        if (productId.HasValue) Input.ProductId = productId.Value;
        if (warehouseId.HasValue) Input.WarehouseId = warehouseId.Value;
        if (!string.IsNullOrWhiteSpace(op)) Input.OperationType = op;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        try
        {
            switch (Input.OperationType.ToLower())
            {
                case "receive":
                    var rec = await _stockService.ReceiveStockAsync(
                        Input.ProductId,
                        Input.WarehouseId,
                        Input.Quantity,
                        Input.Note,
                        Input.PerformedBy);
                    SuccessMessage = $"Příjem {Input.Quantity} ks byl úspěšně zaevidován na skladě (doklad {rec.ReferenceNumber}).";
                    break;

                case "issue":
                    var iss = await _stockService.IssueStockAsync(
                        Input.ProductId,
                        Input.WarehouseId,
                        Input.Quantity,
                        Input.Note,
                        Input.PerformedBy,
                        Input.ReferenceNumber);
                    SuccessMessage = $"Výdej {Input.Quantity} ks byl úspěšně proveden (doklad {iss.ReferenceNumber}).";
                    break;

                case "transfer":
                    if (!Input.TargetWarehouseId.HasValue || Input.TargetWarehouseId.Value <= 0)
                    {
                        ModelState.AddModelError("Input.TargetWarehouseId", "Zvolte cílový sklad pro přesun zboží.");
                        await LoadDropdownsAsync();
                        return Page();
                    }

                    var (outMov, inMov) = await _stockService.TransferStockAsync(
                        Input.ProductId,
                        Input.WarehouseId,
                        Input.TargetWarehouseId.Value,
                        Input.Quantity,
                        Input.Note,
                        Input.PerformedBy);
                    SuccessMessage = $"Přesun {Input.Quantity} ks mezi sklady byl úspěšně dokončen (převodka {outMov.ReferenceNumber}).";
                    break;

                case "adjust":
                    if (!Input.ActualCount.HasValue || Input.ActualCount.Value < 0)
                    {
                        ModelState.AddModelError("Input.ActualCount", "Zadejte skutečný nezáporný počet kusů zjištěný inventurou.");
                        await LoadDropdownsAsync();
                        return Page();
                    }

                    var adj = await _stockService.AdjustStockAsync(
                        Input.ProductId,
                        Input.WarehouseId,
                        Input.ActualCount.Value,
                        Input.Note ?? string.Empty,
                        Input.PerformedBy);
                    SuccessMessage = $"Inventurní úprava stavu byla úspěšně zapsána do auditního logu (doklad {adj.ReferenceNumber}).";
                    break;

                default:
                    throw new DomainException("Neznámý typ operace.");
            }

            return RedirectToPage("./Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDropdownsAsync();
            return Page();
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var products = await _productService.GetAllProductsAsync(onlyActive: true);
        ProductOptions = products.Select(p => new SelectListItem
        {
            Value = p.Id.ToString(),
            Text = $"{p.Code} – {p.Name}"
        }).ToList();

        var warehouses = await _warehouseRepository.GetAllAsync(onlyActive: true);
        WarehouseOptions = warehouses.Select(w => new SelectListItem
        {
            Value = w.Id.ToString(),
            Text = w.Name + (w.IsMain ? " (Centrála)" : "")
        }).ToList();
    }
}

