using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Orders;

public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;

    public DetailsModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public Order Order { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        Order = order;
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync(int id)
    {
        try
        {
            await _orderService.DispatchOrderAsync(id, "Petr Doležal");
            TempData["SuccessMessage"] = "Objednávka byla úspěšně fyzicky expedována. Zásoby byly odepsány a auditní výdej byl zaznamenán.";
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
    {
        try
        {
            await _orderService.CancelOrderAsync(id, reason ?? "Stornováno operátorem");
            TempData["SuccessMessage"] = "Objednávka byla stornována a rezervované zásoby byly uvolněny zpět k dispozici.";
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }
}

