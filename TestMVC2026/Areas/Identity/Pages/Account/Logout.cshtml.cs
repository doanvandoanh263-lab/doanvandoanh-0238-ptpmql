using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class LogoutModel(SignInManager<IdentityUser> signInManager) : PageModel
{
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();
        if (returnUrl is not null && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToPage("/Account/Login", new { area = "Identity" });
    }
}
