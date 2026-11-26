using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Data;
using TestMVC2026.Models;

namespace TestMVC2026.Controllers;

public sealed class StudentController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var query = db.Students.Include(student => student.Faculty).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            query = query.Where(student => student.StudentCode.Contains(search) ||
                student.FullName.Contains(search) || student.Faculty!.Name.Contains(search));
        }
        var count = await query.CountAsync();
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, pages);
        var model = new PagedResult<Student>
        {
            Items = await query.OrderBy(student => student.StudentCode)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Page = page,
            PageSize = pageSize,
            TotalCount = count,
            SearchString = searchString?.Trim()
        };
        return IsAjax() ? PartialView("_StudentList", model) : View(model);
    }

    public async Task<IActionResult> Create()
    {
        await SetFacultiesAsync();
        return PartialView("_StudentForm", new Student());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student)
    {
        student.StudentCode = student.StudentCode.Trim();
        if (await db.Students.AnyAsync(item => item.StudentCode == student.StudentCode))
            ModelState.AddModelError(nameof(student.StudentCode), "Mã sinh viên đã tồn tại.");
        if (!await db.Faculties.AnyAsync(item => item.Code == student.FacultyCode))
            ModelState.AddModelError(nameof(student.FacultyCode), "Vui lòng chọn khoa hợp lệ.");
        if (!ModelState.IsValid)
        {
            await SetFacultiesAsync(student.FacultyCode);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_StudentForm", student);
        }
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã thêm sinh viên {student.FullName}." });
    }

    public async Task<IActionResult> Edit(string id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        await SetFacultiesAsync(student.FacultyCode);
        return PartialView("_StudentForm", student);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Student student)
    {
        if (id != student.StudentCode) return BadRequest();
        if (!await db.Faculties.AnyAsync(item => item.Code == student.FacultyCode))
            ModelState.AddModelError(nameof(student.FacultyCode), "Vui lòng chọn khoa hợp lệ.");
        if (!ModelState.IsValid)
        {
            await SetFacultiesAsync(student.FacultyCode);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_StudentForm", student);
        }
        var existing = await db.Students.FindAsync(id);
        if (existing is null) return NotFound();
        existing.FullName = student.FullName;
        existing.BirthYear = student.BirthYear;
        existing.Email = student.Email;
        existing.Phone = student.Phone;
        existing.FacultyCode = student.FacultyCode;
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã cập nhật sinh viên {student.FullName}." });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        db.Students.Remove(student);
        await db.SaveChangesAsync();
        return Json(new { message = $"Đã xóa sinh viên {student.FullName}." });
    }

    private async Task SetFacultiesAsync(string? selected = null) =>
        ViewBag.Faculties = new SelectList(await db.Faculties.OrderBy(item => item.Name).ToListAsync(),
            nameof(Faculty.Code), nameof(Faculty.Name), selected);

    private bool IsAjax() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
