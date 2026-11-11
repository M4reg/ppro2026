using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Drevenka.Web.Pages.Orders;

public class CreateModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductService _productService;
    private readonly IStockService _stockService;

    public CreateModel(
        IOrderService orderService,
        ICustomerRepository customerRepository,
        IProductService productService,
        IStockService stockService)
    {
        _orderService = orderService;
        _customerRepository = customerRepository;
        _productService = productService;
        _stockService = stockService;
    }

    public List<SelectListItem> CustomerOptions { get; private set; } = [];
    public IReadOnlyList<ProductStockInfo> AvailableProducts { get; private set; } = [];

    public class ProductStockInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableStock { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Zvolte zákazníka nebo vyplňte nového.")]
        [Display(Name = "Zákazník")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Dodací adresa je povinná.")]
        [Display(Name = "Ulice a číslo")]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Město je povinné.")]
        [Display(Name = "Město")]
        public string DeliveryCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "PSČ je povinné.")]
        [Display(Name = "PSČ")]
        public string DeliveryPostalCode { get; set; } = string.Empty;

        [Display(Name = "Poznámka zákazníka")]
        public string? CustomerNote { get; set; }

        public List<OrderItemInput> Items { get; set; } = [];
    }

    public class OrderItemInput
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadFormOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validItems = Input.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .Select(i => new CreateOrderItemDto(i.ProductId, i.Quantity))
            .ToList();

        if (validItems.Count == 0)
        {
            ModelState.AddModelError("", "Vyberte alespoň jeden produkt s množstvím větším než 0.");
        }

        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync();
            return Page();
        }

        try
        {
            var dto = new CreateOrderDto(
                CustomerId: Input.CustomerId,
                DeliveryAddress: Input.DeliveryAddress,
                DeliveryCity: Input.DeliveryCity,
                DeliveryPostalCode: Input.DeliveryPostalCode,
                CustomerNote: Input.CustomerNote,
                Items: validItems);

            var order = await _orderService.CreateOrderAsync(dto);

            TempData["SuccessMessage"] = $"Objednávka {order.OrderNumber} byla úspěšně přijata a zásoby byly zarezervovány na centrálním skladě.";
            return RedirectToPage("./Details", new { id = order.Id });
        }
        catch (InsufficientStockException ex)
        {
            // Přesně plní zadání klienta: odmítnout objednávku při nedostatku zásob
            ModelState.AddModelError("", $"Objednávka byla systémem odmítnuta z důvodu nedostatku zásob! {ex.Message}");
            await LoadFormOptionsAsync();
            return Page();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadFormOptionsAsync();
            return Page();
        }
    }

    private async Task LoadFormOptionsAsync()
    {
        var customers = await _customerRepository.GetAllAsync(onlyActive: true);
        CustomerOptions = customers.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = $"{c.FullName} ({c.City}, {c.Email})",
            Selected = Input.CustomerId == c.Id
        }).ToList();

        var products = await _productService.GetAllProductsAsync(onlyActive: true);
        var stockOverview = await _stockService.GetStockOverviewAsync();

        AvailableProducts = products.Select(p =>
        {
            var stockItems = stockOverview.Where(s => s.ProductId == p.Id).ToList();
            var totalAvailable = stockItems.Sum(s => s.AvailableQuantity);
            return new ProductStockInfo
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Price = p.SellingPrice,
                AvailableStock = totalAvailable
            };
        }).ToList();
    }
}

