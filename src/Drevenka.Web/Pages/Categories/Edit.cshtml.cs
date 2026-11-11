using System.ComponentModel.DataAnnotations;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Drevenka.Web.Pages.Categories;

public class EditModel : PageModel
{
    private readonly ICategoryService _categoryService;

    public EditModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [BindProperty]
    public CategoryEditInputModel Input { get; set; } = new();

    public class CategoryEditInputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Název kategorie je povinný.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Název kategorie musí mít 2 až 100 znaků.")]
        [Display(Name = "Název kategorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Popis nesmí překročit 500 znaků.")]
        [Display(Name = "Popis kategorie")]
        public string? Description { get; set; }

        [Display(Name = "Aktivní kategorie")]
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        Input = new CategoryEditInputModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var category = await _categoryService.UpdateCategoryAsync(Input.Id, Input.Name, Input.Description, Input.IsActive);
            TempData["SuccessMessage"] = $"Kategorie '{category.Name}' byla úspěšně upravena.";
            return RedirectToPage("./Index");
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}

