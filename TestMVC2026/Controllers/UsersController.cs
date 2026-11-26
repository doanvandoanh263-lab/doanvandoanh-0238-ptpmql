using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Data;
using TestMVC2026.Models;

namespace TestMVC2026.Controllers;

public sealed class UsersController(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var userQuery = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            userQuery = userQuery.Where(user => user.Email != null && user.Email.Contains(term));
        }
        var count = await userQuery.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var rows = new List<UserRow>();
        var users = await userQuery.OrderBy(user => user.Email)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        foreach (var user in users)
            rows.Add(new UserRow
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                Roles = string.Join(", ", await userManager.GetRolesAsync(user)),
                EmailConfirmed = user.EmailConfirmed
            });
        var model = new PagedResult<UserRow>
        {
            Items = rows, Page = page, PageSize = pageSize, TotalCount = count, SearchString = searchString?.Trim()
        };
        return IsAjax() ? PartialView("_UserList", model) : View(model);
    }

    public async Task<IActionResult> Roles(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return View(new UserRolesViewModel
        {
            User = user,
            Roles = await roleManager.Roles.Select(role => role.Name!).OrderBy(name => name).ToListAsync(),
            SelectedRoles = (await userManager.GetRolesAsync(user)).ToHashSet(StringComparer.Ordinal)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRoles(string id, List<string>? selectedRoles)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        selectedRoles ??= [];
        if (user.Email == User.Identity?.Name && !selectedRoles.Contains("Admin"))
        {
            TempData["Error"] = "Không thể gỡ quyền Admin của tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Roles), new { id });
        }
        var validRoles = await roleManager.Roles.Select(role => role.Name!).ToListAsync();
        if (selectedRoles.Except(validRoles).Any())
        {
            TempData["Error"] = "Danh sách role không hợp lệ.";
            return RedirectToAction(nameof(Roles), new { id });
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var current = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, current);
        if (!remove.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = string.Join(" ", remove.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(Roles), new { id });
        }
        var add = await userManager.AddToRolesAsync(user, selectedRoles.Distinct());
        if (!add.Succeeded)
        {
            await transaction.RollbackAsync();
        }
        else
        {
            await transaction.CommitAsync();
        }
        TempData[add.Succeeded ? "Success" : "Error"] = add.Succeeded
            ? "Đã cập nhật role cho người dùng."
            : string.Join(" ", add.Errors.Select(error => error.Description));
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
