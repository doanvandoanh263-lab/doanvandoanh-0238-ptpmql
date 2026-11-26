using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class ResendEmailConfirmationModel(
    UserManager<IdentityUser> userManager,
    IEmailSender emailSender) : PageModel
{
    [BindProperty]
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public void OnGet(string? email) => Email = email ?? string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Email);
        if (user is not null)
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page("/Account/ConfirmEmail", "OnGet", new { area = "Identity", userId = user.Id, code = encoded }, Request.Scheme);
            await emailSender.SendEmailAsync(Email, "Xác nhận email",
                $"Vui lòng xác nhận tài khoản <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>tại đây</a>.");
        }
        TempData["Message"] = "Nếu tài khoản tồn tại, email xác thực đã được gửi.";
        return RedirectToPage("./RegisterConfirmation", new { email = Email });
    }
}
