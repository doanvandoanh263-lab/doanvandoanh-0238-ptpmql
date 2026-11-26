using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TestMVC2026.Data;
using TestMVC2026.Models;

namespace TestMVC2026.Controllers;

public sealed class ImportController(
    ApplicationDbContext db,
    RoleManager<IdentityRole> roleManager,
    IActionDescriptorCollectionProvider actions,
    ILogger<ImportController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Template()
    {
        using var workbook = new XLWorkbook();
        var faculties = workbook.Worksheets.Add("Khoa");
        faculties.Cell(1, 1).Value = "Code";
        faculties.Cell(1, 2).Value = "Name";
        faculties.Cell(2, 1).Value = "CNTT";
        faculties.Cell(2, 2).Value = "Công nghệ thông tin";

        var students = workbook.Worksheets.Add("SinhVien");
        var headers = new[] { "StudentCode", "FullName", "BirthYear", "Email", "Phone", "FacultyCode" };
        for (var index = 0; index < headers.Length; index++)
            students.Cell(1, index + 1).Value = headers[index];
        students.Cell(2, 1).Value = "SV001";
        students.Cell(2, 2).Value = "Nguyễn Văn A";
        students.Cell(2, 3).Value = 2005;
        students.Cell(2, 4).Value = "sva@example.com";
        students.Cell(2, 5).Value = "0900000000";
        students.Cell(2, 6).Value = "CNTT";
        var permissions = workbook.Worksheets.Add("RolePermissions");
        permissions.Cell(1, 1).Value = "RoleName";
        permissions.Cell(1, 2).Value = "Controller";
        permissions.Cell(1, 3).Value = "Action";
        faculties.Columns().AdjustToContents();
        students.Columns().AdjustToContents();
        permissions.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapDuLieu.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> Upload(IFormFile? file, string mode, bool confirmed)
    {
        if (!confirmed)
        {
            TempData["Error"] = "Hãy xác nhận chế độ nhập trước khi tải dữ liệu lên.";
            return RedirectToAction(nameof(Index));
        }
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn tệp Excel .xlsx.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > 10_000_000 || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Tệp phải có định dạng .xlsx và dung lượng tối đa 10 MB.";
            return RedirectToAction(nameof(Index));
        }
        if (mode is not ("Append" or "Replace"))
        {
            TempData["Error"] = "Vui lòng chọn chế độ ghi bổ sung hoặc thay thế.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var faculties = ReadFaculties(workbook);
            var students = ReadStudents(workbook);
            var permissions = await ReadPermissionsAsync(workbook);
            var knownFacultyCodes = mode == "Append"
                ? (await db.Faculties.Select(item => item.Code).ToListAsync()).Concat(faculties.Select(item => item.Code))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : faculties.Select(item => item.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var student in students)
            {
                if (!knownFacultyCodes.Contains(student.FacultyCode))
                    throw new ImportValidationException($"Mã khoa '{student.FacultyCode}' của sinh viên {student.StudentCode} không tồn tại trong dữ liệu.");
            }

            if (mode == "Append")
            {
                var existingFacultyCodes = await db.Faculties.Select(item => item.Code).ToListAsync();
                var existingStudentCodes = await db.Students.Select(item => item.StudentCode).ToListAsync();
                var existingPermissions = await db.RolePermissions.ToListAsync();
                if (faculties.Any(item => existingFacultyCodes.Contains(item.Code, StringComparer.OrdinalIgnoreCase)) ||
                    students.Any(item => existingStudentCodes.Contains(item.StudentCode, StringComparer.OrdinalIgnoreCase)) ||
                    permissions.Any(item => existingPermissions.Any(existing =>
                        existing.RoleId == item.RoleId &&
                        existing.ControllerName == item.ControllerName &&
                        existing.ActionName == item.ActionName)))
                    throw new ImportValidationException("Chế độ ghi bổ sung không cho phép trùng mã khoa, mã sinh viên hoặc quyền đã có.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            if (mode == "Replace")
            {
                db.Students.RemoveRange(await db.Students.ToListAsync());
                db.Faculties.RemoveRange(await db.Faculties.ToListAsync());
                db.RolePermissions.RemoveRange(await db.RolePermissions.ToListAsync());
                await db.SaveChangesAsync();
            }
            db.Faculties.AddRange(faculties);
            db.Students.AddRange(students);
            db.RolePermissions.AddRange(permissions);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã nhập {faculties.Count} khoa, {students.Count} sinh viên và {permissions.Count} quyền theo chế độ {(mode == "Replace" ? "thay thế" : "ghi bổ sung")}.";
        }
        catch (ImportValidationException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Invalid workbook uploaded by {User}", User.Identity?.Name);
            TempData["Error"] = "Không đọc được tệp Excel. Hãy dùng tệp mẫu .xlsx và kiểm tra tên trang tính/cột.";
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Excel import violated database constraints for {User}", User.Identity?.Name);
            TempData["Error"] = "Dữ liệu không hợp lệ hoặc bị trùng khóa. Không có dữ liệu nào được nhập.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static List<Faculty> ReadFaculties(XLWorkbook workbook)
    {
        var sheet = GetSheet(workbook, "Khoa", ["Code", "Name"]);
        var result = new List<Faculty>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            if (sheet.Row(row).IsEmpty()) continue;
            var faculty = new Faculty { Code = Cell(sheet, row, "Code"), Name = Cell(sheet, row, "Name") };
            Validate(faculty, "Khoa", row);
            if (!seen.Add(faculty.Code)) throw new ImportValidationException($"Trang Khoa, dòng {row}: mã khoa bị trùng.");
            result.Add(faculty);
        }
        return result;
    }

    private static List<Student> ReadStudents(XLWorkbook workbook)
    {
        var sheet = GetSheet(workbook, "SinhVien", ["StudentCode", "FullName", "BirthYear", "Email", "Phone", "FacultyCode"]);
        var result = new List<Student>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            if (sheet.Row(row).IsEmpty()) continue;
            var student = new Student
            {
                StudentCode = Cell(sheet, row, "StudentCode"),
                FullName = Cell(sheet, row, "FullName"),
                BirthYear = int.TryParse(Cell(sheet, row, "BirthYear"), out var year) ? year : 0,
                Email = EmptyToNull(Cell(sheet, row, "Email")),
                Phone = EmptyToNull(Cell(sheet, row, "Phone")),
                FacultyCode = Cell(sheet, row, "FacultyCode")
            };
            Validate(student, "SinhVien", row);
            if (!seen.Add(student.StudentCode)) throw new ImportValidationException($"Trang SinhVien, dòng {row}: mã sinh viên bị trùng.");
            result.Add(student);
        }
        return result;
    }

    private async Task<List<RolePermission>> ReadPermissionsAsync(XLWorkbook workbook)
    {
        var sheet = GetSheet(workbook, "RolePermissions", ["RoleName", "Controller", "Action"]);
        var roleMap = await roleManager.Roles.ToDictionaryAsync(role => role.Name!, role => role.Id);
        var validActions = actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
            .Where(item => item.ControllerName != "Home")
            .Select(item => item.ControllerName + "." + item.ActionName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<RolePermission>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            if (sheet.Row(row).IsEmpty()) continue;
            var roleName = Cell(sheet, row, "RoleName");
            var controller = Cell(sheet, row, "Controller");
            var action = Cell(sheet, row, "Action");
            if (!roleMap.TryGetValue(roleName, out var roleId))
                throw new ImportValidationException($"Trang RolePermissions, dòng {row}: role '{roleName}' không tồn tại.");
            if (!validActions.Contains($"{controller}.{action}"))
                throw new ImportValidationException($"Trang RolePermissions, dòng {row}: action '{controller}.{action}' không tồn tại.");
            if (!seen.Add($"{roleId}|{controller}|{action}"))
                throw new ImportValidationException($"Trang RolePermissions, dòng {row}: quyền bị trùng.");
            result.Add(new RolePermission { RoleId = roleId, ControllerName = controller, ActionName = action });
        }
        return result;
    }

    private static IXLWorksheet GetSheet(XLWorkbook workbook, string name, string[] headers)
    {
        if (!workbook.TryGetWorksheet(name, out var sheet))
            throw new ImportValidationException($"Thiếu trang tính '{name}'.");
        var headerCells = sheet.Row(1).CellsUsed().ToList();
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerCells)
        {
            var headerName = cell.GetString().Trim();
            if (!headerMap.TryAdd(headerName, cell.Address.ColumnNumber))
                throw new ImportValidationException($"Trang '{sheet.Name}' có cột tiêu đề bị trùng.");
        }
        var missing = headers.Where(header => !headerMap.ContainsKey(header)).ToList();
        if (missing.Count > 0) throw new ImportValidationException($"Trang '{name}' thiếu cột: {string.Join(", ", missing)}.");
        return sheet;
    }

    private static string Cell(IXLWorksheet sheet, int row, string header)
    {
        var column = sheet.Row(1).CellsUsed()
            .First(cell => string.Equals(cell.GetString().Trim(), header, StringComparison.OrdinalIgnoreCase))
            .Address.ColumnNumber;
        return sheet.Cell(row, column).GetFormattedString().Trim();
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static void Validate(object model, string sheet, int row)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
            throw new ImportValidationException($"Trang {sheet}, dòng {row}: {string.Join(" ", results.Select(result => result.ErrorMessage))}");
    }

    private sealed class ImportValidationException(string message) : Exception(message);
}
