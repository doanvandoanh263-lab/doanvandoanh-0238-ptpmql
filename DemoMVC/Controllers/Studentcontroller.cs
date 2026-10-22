using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers;

public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;

    // Dependency Injection: Nhận ApplicationDbContext để tương tác với CSDL
    public StudentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================================
    // 1. READ (ĐỌC / TRUY VẤN DỮ LIỆU)
    // ==========================================================

    // GET: /Student hoặc /Student?searchString=abc
    // Lấy toàn bộ danh sách sinh viên hoặc tìm kiếm theo từ khóa
    public async Task<IActionResult> Index(string? searchString)
    {
        // Khởi tạo truy vấn IQueryable từ DbSet Students (chưa thực thi câu lệnh SQL)
        var query = _context.Students.AsQueryable();

        // Lọc dữ liệu theo tên hoặc trường nếu người dùng nhập từ khóa tìm kiếm
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            query = query.Where(s => s.FullName.Contains(searchString) || 
                                     (s.School != null && s.School.Contains(searchString)));
            ViewBag.CurrentSearch = searchString;
        }

        // Thực thi truy vấn SQL (SELECT * FROM Students WHERE ... ORDER BY Id DESC)
        var students = await query.OrderByDescending(s => s.Id).ToListAsync();
        return View(students);
    }

    // GET: /Student/Details/5
    // Đọc thông tin chi tiết của 1 sinh viên theo ID
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // EF Core: Tìm kiếm bản ghi đầu tiên khớp với điều kiện Id
        var student = await _context.Students
            .FirstOrDefaultAsync(m => m.Id == id);

        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // ==========================================================
    // 2. CREATE (TẠO MỚI DỮ LIỆU)
    // ==========================================================

    // GET: /Student/Create
    // Trả về giao diện form tạo mới
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Student/Create
    // Tiếp nhận dữ liệu từ Form và thêm mới vào Database
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FullName,Address,School,Gender")] Student student)
    {
        if (ModelState.IsValid)
        {
            // EF Core: Đưa thực thể student vào Change Tracker với trạng thái EntityState.Added
            _context.Students.Add(student);

            // EF Core: Sinh lệnh SQL INSERT INTO Students (...) VALUES (...) và thực thi
await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm sinh viên \"{student.FullName}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Nếu dữ liệu không hợp lệ, tải lại form cùng thông báo lỗi
        return View(student);
    }

    // ==========================================================
    // 3. UPDATE (CẬP NHẬT DỮ LIỆU)
    // ==========================================================

    // GET: /Student/Edit/5
    // Lấy thông tin sinh viên hiện tại theo Id và đổ lên Form chỉnh sửa
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // EF Core: FindAsync tìm kiếm nhanh theo Khóa chính (Primary Key)
        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: /Student/Edit/5
    // Nhận thông tin đã sửa từ Form và lưu cập nhật vào CSDL
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Address,School,Gender")] Student student)
    {
        if (id != student.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // EF Core: Đánh dấu trạng thái thực thể là EntityState.Modified
                _context.Students.Update(student);

                // EF Core: Sinh lệnh SQL UPDATE Students SET ... WHERE Id = ...
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật sinh viên \"{student.FullName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Xử lý lỗi tranh chấp dữ liệu khi bản ghi đã bị xóa/thay đổi bởi tiến trình khác
                if (!StudentExists(student.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        return View(student);
    }

    // ==========================================================
    // 4. DELETE (XÓA DỮ LIỆU)
    // ==========================================================

    // POST: /Student/Delete/5
    // Xóa một sinh viên theo ID
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // EF Core: Tìm bản ghi cần xóa
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            // EF Core: Đánh dấu trạng thái thực thể là EntityState.Deleted
            _context.Students.Remove(student);
// EF Core: Sinh lệnh SQL DELETE FROM Students WHERE Id = ...
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa sinh viên \"{student.FullName}\" thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không tìm thấy sinh viên cần xóa!";
        }

        return RedirectToAction(nameof(Index));
    }

    // Phương thức phụ: Kiểm tra sinh viên có tồn tại trong CSDL hay không
    private bool StudentExists(int id)
    {
        return _context.Students.Any(e => e.Id == id);
    }
}