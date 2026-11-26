using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class ResetPasswordModel(UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet(string? code = null)
    {
        if (code is null) return BadRequest("Thiếu mã đặt lại mật khẩu.");
        Input.Code = code;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null) return RedirectToPage("./ResetPasswordConfirmation");
        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        var result = await userManager.ResetPasswordAsync(user, code, Input.Password);
        if (result.Succeeded) return RedirectToPage("./ResetPasswordConfirmation");
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return Page();
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 10)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
