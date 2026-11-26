using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Data;
using TestMVC2026.Models;

namespace TestMVC2026.Controllers;

public sealed class RolesController(
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db,
    IActionDescriptorCollectionProvider actions) : Controller
{
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var roleQuery = roleManager.Roles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            roleQuery = roleQuery.Where(role => role.Name!.Contains(term));
        }
        var count = await roleQuery.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var rows = new List<RoleRow>();
        var roles = await roleQuery.OrderBy(role => role.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        foreach (var role in roles)
        {
            var userCount = await db.UserRoles.CountAsync(item => item.RoleId == role.Id);
            rows.Add(new RoleRow { Id = role.Id, Name = role.Name ?? string.Empty, UserCount = userCount });
        }
        var model = new PagedResult<RoleRow>
        {
            Items = rows, Page = page, PageSize = pageSize, TotalCount = count, SearchString = searchString?.Trim()
        };
        return IsAjax() ? PartialView("_RoleList", model) : View(model);
    }

    public IActionResult Create() => PartialView("_RoleForm", new RoleFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Vui lòng nhập tên role.");
            return InvalidRoleForm(model);
        }
        var result = await roleManager.CreateAsync(new IdentityRole(model.Name));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Name), string.Join(" ", result.Errors.Select(error => error.Description)));
            return InvalidRoleForm(model);
        }
        return MutationResult($"Đã tạo role {model.Name}.");
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.Name == "Admin") return Forbid();
        return PartialView("_RoleForm", new RoleFormViewModel { Id = role.Id, Name = role.Name ?? string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Id)) return BadRequest();
        var role = await roleManager.FindByIdAsync(model.Id);
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (role is null) return NotFound();
        if (role.Name == "Admin")
        {
            return Forbid();
        }
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Vui lòng nhập tên role.");
            return InvalidRoleForm(model);
        }
        role.Name = model.Name;
        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Name), string.Join(" ", result.Errors.Select(error => error.Description)));
            return InvalidRoleForm(model);
        }
        return MutationResult($"Đã cập nhật role {model.Name}.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        if (role.Name == "Admin")
        {
            return Forbid();
        }
        if (await db.UserRoles.AnyAsync(item => item.RoleId == id))
        {
            const string error = "Hãy gỡ role khỏi tất cả người dùng trước khi xóa.";
            return IsAjax() ? Conflict(new { message = error }) : RedirectToAction(nameof(Index), new { error });
        }
        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(error => error.Description));
            return IsAjax() ? BadRequest(new { message }) : RedirectToAction(nameof(Index), new { error = message });
        }
        return MutationResult($"Đã xóa role {role.Name}.");
    }

    public async Task<IActionResult> Permissions(string roleId, string? searchString, int page = 1)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return NotFound();

        var granted = await db.RolePermissions.Where(item => item.RoleId == roleId)
            .Select(item => item.ControllerName + "." + item.ActionName).ToListAsync();
        var available = actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .Select(action => new { action.ControllerName, action.ActionName })
            .Where(item => item.ControllerName is not "Home")
            .Distinct()
            .OrderBy(item => item.ControllerName).ThenBy(item => item.ActionName);
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            available = available.Where(item => item.ControllerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.ActionName.Contains(term, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.ControllerName).ThenBy(item => item.ActionName);
        }
        const int pageSize = 20;
        var all = available.ToList();
        var count = all.Count;
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        ViewBag.Role = role;
        ViewBag.SelectedActions = granted.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return View(new PagedResult<PermissionRow>
        {
            Items = all.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new PermissionRow
                {
                    Controller = item.ControllerName,
                    Action = item.ActionName,
                    Granted = granted.Contains(item.ControllerName + "." + item.ActionName)
                }).ToList(),
            Page = page, PageSize = pageSize, TotalCount = count, SearchString = searchString?.Trim()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(PermissionUpdateModel model)
    {
        var role = await roleManager.FindByIdAsync(model.RoleId);
        if (role is null) return NotFound();
        if (role.Name == "Admin")
        {
            TempData["Error"] = "Role Admin luôn có toàn quyền; không thể thay đổi phân quyền.";
            return RedirectToAction(nameof(Permissions), new { roleId = model.RoleId });
        }

        var allowed = actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .Where(item => item.ControllerName != "Home")
            .Select(item => item.ControllerName + "." + item.ActionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (model.SelectedActions.Any(item => !allowed.Contains(item)))
        {
            ModelState.AddModelError(string.Empty, "Danh sách action gửi lên không hợp lệ.");
            return BadRequest(ModelState);
        }
        var existing = await db.RolePermissions.Where(item => item.RoleId == model.RoleId).ToListAsync();
        db.RolePermissions.RemoveRange(existing);
        foreach (var value in model.SelectedActions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parts = value.Split('.', 2);
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = model.RoleId,
                ControllerName = parts[0],
                ActionName = parts[1]
            });
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu quyền theo action.";
        return RedirectToAction(nameof(Permissions), new { roleId = model.RoleId, searchString = Request.Form["searchString"], page = Request.Form["page"] });
    }

    private IActionResult InvalidRoleForm(RoleFormViewModel model)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_RoleForm", model);
    }

    private IActionResult MutationResult(string message)
    {
        if (IsAjax()) return Json(new { message });
        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
