using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Categories;

public class CreateModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public CreateModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [BindProperty]
    public CategoryInputModel Input { get; set; } = new();

    public class CategoryInputModel
    {
        [Required(ErrorMessage = "Název kategorie je povinný.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Název kategorie musí mít 2 až 100 znaků.")]
        [Display(Name = "Název kategorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Popis nesmí překročit 500 znaků.")]
        [Display(Name = "Popis kategorie")]
        public string? Description { get; set; }
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
            var category = await _categoryService.CreateCategoryAsync(Input.Name, Input.Description);
            TempData["SuccessMessage"] = $"Kategorie '{category.Name}' byla úspěšně vytvořena.";
            return RedirectToPage("./Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}

