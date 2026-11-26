using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public class KhoaController : Controller
{
    private readonly ApplicationDbContext _context;

    public KhoaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? searchString, int page = 1)
    {
        const int pageSize = 10;
        var khoas = _context.Khoas
            .Include(khoa => khoa.Students)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            khoas = khoas.Where(khoa => khoa.Code.Contains(search) || khoa.Name.Contains(search));
        }

        var totalCount = await khoas.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);
        var result = new PagedResult<Khoa>
        {
            Items = await khoas.OrderBy(khoa => khoa.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            SearchString = searchString?.Trim()
        };

        return IsAjaxRequest()
            ? PartialView("_KhoaList", result)
            : View(result);
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var khoa = await _context.Khoas.Include(item => item.Students)
            .FirstOrDefaultAsync(item => item.Code == id);
        if (khoa == null)
        {
            return NotFound();
        }

        return IsAjaxRequest()
            ? PartialView("_KhoaDetailsModal", khoa)
            : View(khoa);
    }

    public IActionResult Create()
    {
        var khoa = new Khoa();
        return IsAjaxRequest()
            ? PartialView("_KhoaFormModal", khoa)
            : View(khoa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Code,Name")] Khoa khoa)
    {
        khoa.Code = khoa.Code?.Trim() ?? string.Empty;
        khoa.Name = khoa.Name?.Trim() ?? string.Empty;
        if (await _context.Khoas.AnyAsync(item => item.Code == khoa.Code))
        {
            ModelState.AddModelError(nameof(khoa.Code), "Mã khoa đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return InvalidForm(khoa, nameof(Create));
        }

        _context.Khoas.Add(khoa);
        await _context.SaveChangesAsync();
        return MutationResult($"Đã thêm khoa {khoa.Name}.");
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var khoa = await _context.Khoas.FindAsync(id);
        if (khoa == null)
        {
            return NotFound();
        }

        return IsAjaxRequest()
            ? PartialView("_KhoaFormModal", khoa)
            : View(khoa);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Code,Name")] Khoa input)
    {
        if (id != input.Code)
        {
            return NotFound();
        }

        input.Code = input.Code?.Trim() ?? string.Empty;
        input.Name = input.Name?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
        {
            return InvalidForm(input, nameof(Edit));
        }

        var khoa = await _context.Khoas.FindAsync(id);
        if (khoa == null)
        {
            return NotFound();
        }

        khoa.Name = input.Name;
        await _context.SaveChangesAsync();
        return MutationResult($"Đã cập nhật khoa {khoa.Name}.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var khoa = await _context.Khoas.FindAsync(id);
        if (khoa == null)
        {
            return NotFound();
        }

        if (await _context.Students.AnyAsync(student => student.KhoaCode == id))
        {
            const string error = "Không thể xóa khoa đang có sinh viên trực thuộc.";
            return IsAjaxRequest()
                ? Conflict(new { success = false, message = error })
                : RedirectToAction(nameof(Index), new { error });
        }

        _context.Khoas.Remove(khoa);
        await _context.SaveChangesAsync();
        return MutationResult($"Đã xóa khoa {khoa.Name}.");
    }

    private IActionResult InvalidForm(Khoa khoa, string action)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        if (IsAjaxRequest())
        {
            return PartialView("_KhoaFormModal", khoa);
        }

        return View(action, khoa);
    }

    private IActionResult MutationResult(string message)
    {
        if (IsAjaxRequest())
        {
            return Json(new { success = true, message });
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
