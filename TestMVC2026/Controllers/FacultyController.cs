using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Data;
using TestMVC2026.Models;

namespace TestMVC2026.Controllers;

public sealed class FacultyController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var query = db.Faculties.Include(faculty => faculty.Students).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var term = searchString.Trim();
            query = query.Where(faculty => faculty.Code.Contains(term) || faculty.Name.Contains(term));
        }
        var count = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(count / (double)pageSize)));
        var model = new PagedResult<Faculty>
        {
            Items = await query.OrderBy(item => item.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Page = page,
            PageSize = pageSize,
            TotalCount = count,
            SearchString = searchString?.Trim()
        };
        return IsAjax() ? PartialView("_FacultyList", model) : View(model);
    }

    public IActionResult Create() => PartialView("_FacultyForm", new Faculty());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Faculty faculty)
    {
        faculty.Code = faculty.Code.Trim();
        faculty.Name = faculty.Name.Trim();
        if (await db.Faculties.AnyAsync(item => item.Code == faculty.Code))
            ModelState.AddModelError(nameof(faculty.Code), "Mã khoa đã tồn tại.");
        if (!ModelState.IsValid) return InvalidForm(faculty);
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã thêm khoa {faculty.Name}." });
    }

    public async Task<IActionResult> Edit(string id)
    {
        var faculty = await db.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : PartialView("_FacultyForm", faculty);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Faculty faculty)
    {
        if (id != faculty.Code) return BadRequest();
        if (!ModelState.IsValid) return InvalidForm(faculty);
        var existing = await db.Faculties.FindAsync(id);
        if (existing is null) return NotFound();
        existing.Name = faculty.Name.Trim();
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã cập nhật khoa {faculty.Name}." });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var faculty = await db.Faculties.FindAsync(id);
        if (faculty is null) return NotFound();
        if (await db.Students.AnyAsync(student => student.FacultyCode == id))
            return Conflict(new { message = "Không thể xóa khoa đang có sinh viên." });
        db.Faculties.Remove(faculty);
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã xóa khoa {faculty.Name}." });
    }

    private IActionResult InvalidForm(Faculty faculty)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_FacultyForm", faculty);
    }

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
