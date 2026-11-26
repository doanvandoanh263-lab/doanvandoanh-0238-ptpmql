using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace TestMVC2026.Areas.Identity.Pages.Account;

public sealed class ConfirmEmailModel(UserManager<IdentityUser> userManager) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code)
    {
        if (userId is null || code is null)
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound($"Không tìm thấy người dùng có mã '{userId}'.");

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);
        StatusMessage = result.Succeeded
            ? "Email đã được xác thực. Bây giờ bạn có thể đăng nhập."
            : "Lỗi khi xác thực email.";
        return Page();
    }
}
