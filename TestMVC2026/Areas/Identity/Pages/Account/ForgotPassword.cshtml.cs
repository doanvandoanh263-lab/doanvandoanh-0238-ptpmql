using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class ForgotPasswordModel(
    UserManager<IdentityUser> userManager,
    IEmailSender emailSender) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is not null)
        {
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page("/Account/ResetPassword", "OnGet", new { area = "Identity", code }, Request.Scheme);
            await emailSender.SendEmailAsync(Input.Email, "Đặt lại mật khẩu",
                $"Vui lòng đặt lại mật khẩu <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>tại đây</a>.");
        }
        return RedirectToPage("./ForgotPasswordConfirmation");
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
