using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly ICustomerRepository _customerRepository;

    public IndexModel(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public IReadOnlyList<Customer> Customers { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public int TotalCustomersCount => Customers.Count;
    public int ActiveCustomersCount => Customers.Count(c => c.IsActive);

    public async Task OnGetAsync()
    {
        Customers = await _customerRepository.GetAllAsync(SearchTerm, onlyActive: false);
    }
}

