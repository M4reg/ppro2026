using Drevenka.Domain.Entities;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Orders;

public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IReadOnlyList<Order> Orders { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public OrderStatus? Status { get; set; }

    public int TotalOrdersCount => Orders.Count;
    public int ConfirmedCount => Orders.Count(o => o.Status == OrderStatus.Confirmed);
    public int DispatchedCount => Orders.Count(o => o.Status == OrderStatus.Dispatched);
    public decimal TotalRevenue => Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount);

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Orders = await _orderService.GetAllOrdersAsync(Status, SearchTerm);
    }
}

