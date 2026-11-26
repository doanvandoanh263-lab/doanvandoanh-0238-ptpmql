using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class RegisterConfirmationModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public void OnGet() { }
}
