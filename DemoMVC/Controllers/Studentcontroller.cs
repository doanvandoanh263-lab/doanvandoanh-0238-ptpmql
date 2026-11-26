using System.Data;
using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var students = _context.Students
            .Include(student => student.Khoa)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            students = students.Where(student =>
                student.FullName.Contains(search) ||
                student.StudentCode.Contains(search) ||
                student.Khoa!.Name.Contains(search) ||
                student.KhoaCode.Contains(search));
        }

        var totalCount = await students.CountAsync();
        var totalStudentCount = await _context.Students.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        var studentList = await students
            .OrderBy(student => student.StudentCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResult<Student>
        {
            Items = studentList,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchString = searchString?.Trim()
        };
        ViewBag.StudentCount = totalStudentCount;
        ViewBag.KhoaCount = await _context.Khoas.CountAsync();

        return IsAjaxRequest()
            ? PartialView("_StudentList", result)
            : View(result);
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var student = await _context.Students
            .Include(item => item.Khoa)
            .FirstOrDefaultAsync(item => item.StudentCode == id);

        if (student == null)
        {
            return NotFound();
        }

        return IsAjaxRequest()
            ? PartialView("_StudentDetailsModal", student)
            : View(student);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateKhoaListAsync();
        var student = new Student();
        return IsAjaxRequest()
            ? PartialView("_StudentFormModal", student)
            : View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("FullName,EntryYear,TrainingSystemCode,MajorCode,Address,KhoaCode")] Student student)
    {
        student.FullName = student.FullName?.Trim() ?? string.Empty;
        if (!await _context.Khoas.AnyAsync(khoa => khoa.Code == student.KhoaCode))
        {
            ModelState.AddModelError(nameof(student.KhoaCode), "Khoa được chọn không tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateKhoaListAsync(student.KhoaCode);
            return IsAjaxRequest()
                ? InvalidForm("_StudentFormModal", student)
                : View(student);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var prefix = CreatePrefix(student);
        var existingCodes = await _context.Students
            .Where(item => item.StudentCode.StartsWith(prefix))
            .Select(item => item.StudentCode)
            .ToListAsync();
        var highestStudentNumber = existingCodes
            .Select(code => int.TryParse(code.AsSpan(6), out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        var counter = await _context.StudentCodeCounters
            .SingleOrDefaultAsync(item => item.Prefix == prefix);
        var lastNumber = Math.Max(highestStudentNumber, counter?.LastNumber ?? 0);
        if (lastNumber >= 9999)
        {
            ModelState.AddModelError(string.Empty, "Dãy mã sinh viên đã vượt quá 4 chữ số cuối.");
            await transaction.RollbackAsync();
            await PopulateKhoaListAsync(student.KhoaCode);
            return IsAjaxRequest()
                ? InvalidForm("_StudentFormModal", student)
                : View(student);
        }

        var nextNumber = lastNumber + 1;
        student.StudentCode = $"{prefix}{nextNumber:0000}";

        if (counter == null)
        {
            _context.StudentCodeCounters.Add(new StudentCodeCounter
            {
                Prefix = prefix,
                LastNumber = nextNumber
            });
        }
        else
        {
            counter.LastNumber = nextNumber;
        }

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var message = $"Đã thêm sinh viên {student.FullName} với mã {student.StudentCode}.";
        if (IsAjaxRequest())
        {
            return Json(new { success = true, message });
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        await PopulateKhoaListAsync(student.KhoaCode);
        return IsAjaxRequest()
            ? PartialView("_StudentFormModal", student)
            : View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        [Bind("StudentCode,FullName,EntryYear,TrainingSystemCode,MajorCode,Address,KhoaCode")] Student student)
    {
        if (id != student.StudentCode)
        {
            return NotFound();
        }

        if (!await _context.Khoas.AnyAsync(khoa => khoa.Code == student.KhoaCode))
        {
            ModelState.AddModelError(nameof(student.KhoaCode), "Khoa được chọn không tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateKhoaListAsync(student.KhoaCode);
            return IsAjaxRequest()
                ? InvalidForm("_StudentFormModal", student)
                : View(student);
        }

        try
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Students.AnyAsync(item => item.StudentCode == student.StudentCode))
            {
                return NotFound();
            }

            throw;
        }

        var message = $"Đã cập nhật sinh viên {student.FullName}.";
        if (IsAjaxRequest())
        {
            return Json(new { success = true, message });
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
        var message = $"Đã xóa sinh viên {student.FullName}.";
        if (IsAjaxRequest())
        {
            return Json(new { success = true, message });
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateKhoaListAsync(string? selectedCode = null)
    {
        ViewBag.KhoaCode = new SelectList(
            await _context.Khoas.OrderBy(khoa => khoa.Name).ToListAsync(),
            nameof(Khoa.Code),
            nameof(Khoa.Name),
            selectedCode);
    }

    private static string CreatePrefix(Student student) =>
        $"{student.EntryYear % 100:00}{student.TrainingSystemCode}{student.MajorCode}";

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private IActionResult InvalidForm(string viewName, Student student)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView(viewName, student);
    }
}
