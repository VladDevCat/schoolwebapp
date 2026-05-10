using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using swa.Data;
using swa.Models;

namespace swa.Controllers;

[Authorize(Roles = "Teacher")]
public class StudentsController : Controller
{
    private readonly SchoolTaskContext _context;

    public StudentsController(SchoolTaskContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? className)
    {
        var students = await _context.Students
            .AsNoTracking()
            .Where(student => string.IsNullOrWhiteSpace(className) || student.ClassName == className)
            .OrderBy(student => student.ClassName)
            .ThenBy(student => student.FullName)
            .ToListAsync();

        ViewBag.Classes = await _context.Students
            .AsNoTracking()
            .Select(student => student.ClassName)
            .Distinct()
            .OrderBy(item => item)
            .ToListAsync();
        ViewBag.SelectedClass = className;

        return View(students);
    }

    public IActionResult Import()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Выберите Excel-файл .xlsx");
            return View();
        }

        var imported = 0;
        await using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RowsUsed().ToList();

        foreach (var row in rows.Skip(HasHeader(rows.First()) ? 1 : 0))
        {
            var fullName = row.Cell(1).GetString().Trim();
            var className = row.Cell(2).GetString().Trim();
            var email = row.Cell(3).GetString().Trim();
            var login = row.Cell(4).GetString().Trim();
            var password = row.Cell(5).GetString().Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            var exists = await _context.Students.AnyAsync(student =>
                student.FullName == fullName && student.ClassName == className);

            if (exists)
            {
                continue;
            }

            login = string.IsNullOrWhiteSpace(login)
                ? await BuildUniqueLoginAsync(fullName, className)
                : login;
            password = string.IsNullOrWhiteSpace(password) ? "student123" : password;

            _context.Students.Add(new Student
            {
                FullName = fullName,
                ClassName = className,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Login = login,
                PasswordHash = PasswordHasher.Hash(password)
            });
            imported++;
        }

        await _context.SaveChangesAsync();
        TempData["ImportResult"] = $"Импортировано учеников: {imported}";

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> BuildUniqueLoginAsync(string fullName, string className)
    {
        var baseLogin = NormalizeLogin(className + "_" + fullName);
        var login = baseLogin;
        var suffix = 1;

        while (await _context.Students.AnyAsync(student => student.Login == login))
        {
            suffix++;
            login = $"{baseLogin}{suffix}";
        }

        return login;
    }

    private static string NormalizeLogin(string value)
    {
        var builder = new StringBuilder();

        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != '.')
            {
                builder.Append('.');
            }
        }

        return builder.ToString().Trim('.').Replace("..", ".");
    }

    private static bool HasHeader(IXLRow row)
    {
        var first = row.Cell(1).GetString().Trim().ToLowerInvariant();
        var second = row.Cell(2).GetString().Trim().ToLowerInvariant();
        return first.Contains("фио") || first.Contains("учен") || second.Contains("класс");
    }
}
