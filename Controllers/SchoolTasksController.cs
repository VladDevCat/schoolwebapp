using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;
using swa.Data;
using swa.Models;

namespace swa.Controllers;

public class SchoolTasksController : Controller
{
    private readonly SchoolTaskContext _context;

    public SchoolTasksController(SchoolTaskContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? subject, SchoolTaskStatus? status)
    {
        var query = _context.SchoolTasks
            .Include(task => task.AssignedStudent)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(task =>
                task.Title.Contains(search) ||
                task.Subject.Contains(search) ||
                (task.Description != null && task.Description.Contains(search)) ||
                (task.AssignedStudent != null && task.AssignedStudent.FullName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            query = query.Where(task => task.Subject == subject);
        }

        if (status.HasValue)
        {
            query = query.Where(task => task.Status == status.Value);
        }

        var today = DateTime.Today;
        var allTasks = await _context.SchoolTasks.AsNoTracking().ToListAsync();

        var viewModel = new SchoolTaskDashboardViewModel
        {
            Tasks = await query
                .OrderBy(task => task.Status == SchoolTaskStatus.Completed)
                .ThenBy(task => task.DueDate)
                .ThenByDescending(task => task.Priority)
                .ToListAsync(),
            Search = search,
            Subject = subject,
            Status = status,
            Subjects = allTasks
                .Select(task => task.Subject)
                .Distinct()
                .OrderBy(item => item)
                .ToList(),
            TotalCount = allTasks.Count,
            CompletedCount = allTasks.Count(task => task.Status == SchoolTaskStatus.Completed),
            DueSoonCount = allTasks.Count(task => task.Status != SchoolTaskStatus.Completed && task.DueDate >= today && task.DueDate <= today.AddDays(3)),
            OverdueCount = allTasks.Count(task => task.Status != SchoolTaskStatus.Completed && task.DueDate < today)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        var schoolTask = await _context.SchoolTasks
            .Include(task => task.AssignedStudent)
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id);

        if (schoolTask is null)
        {
            return NotFound();
        }

        return View(schoolTask);
    }

    public async Task<IActionResult> Calendar()
    {
        var tasks = await _context.SchoolTasks
            .Include(task => task.AssignedStudent)
            .AsNoTracking()
            .OrderBy(task => task.DueDate)
            .ThenByDescending(task => task.Priority)
            .ToListAsync();

        return View(tasks);
    }

    public async Task<IActionResult> ExportCsv()
    {
        var tasks = await _context.SchoolTasks
            .Include(task => task.AssignedStudent)
            .AsNoTracking()
            .OrderBy(task => task.DueDate)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Название;Предмет;Преподаватель;Ученик;Класс;Срок;Приоритет;Статус;Описание");

        foreach (var task in tasks)
        {
            csv.AppendLine(string.Join(';',
                EscapeCsv(task.Title),
                EscapeCsv(task.Subject),
                EscapeCsv(task.Teacher ?? string.Empty),
                EscapeCsv(task.AssignedStudent?.FullName ?? string.Empty),
                EscapeCsv(task.AssignedStudent?.ClassName ?? string.Empty),
                task.DueDate.ToString("dd.MM.yyyy"),
                task.Priority,
                task.Status,
                EscapeCsv(task.Description ?? string.Empty)));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "school-tasks.csv");
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create()
    {
        await LoadStudentsAsync();
        return View(new SchoolTask { DueDate = DateTime.Today.AddDays(1) });
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SchoolTask schoolTask)
    {
        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();
            return View(schoolTask);
        }

        schoolTask.CreatedAt = DateTime.UtcNow;
        _context.Add(schoolTask);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Edit(int id)
    {
        var schoolTask = await _context.SchoolTasks.FindAsync(id);
        if (schoolTask is null)
        {
            return NotFound();
        }

        await LoadStudentsAsync();
        return View(schoolTask);
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SchoolTask schoolTask)
    {
        if (id != schoolTask.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadStudentsAsync();
            return View(schoolTask);
        }

        _context.Update(schoolTask);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var schoolTask = await _context.SchoolTasks.FindAsync(id);
        if (schoolTask is null)
        {
            return NotFound();
        }

        schoolTask.Status = schoolTask.Status == SchoolTaskStatus.Completed
            ? SchoolTaskStatus.InProgress
            : SchoolTaskStatus.Completed;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var schoolTask = await _context.SchoolTasks.FindAsync(id);
        if (schoolTask is null)
        {
            return NotFound();
        }

        _context.SchoolTasks.Remove(schoolTask);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadStudentsAsync()
    {
        var students = await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.ClassName)
            .ThenBy(student => student.FullName)
            .Select(student => new
            {
                student.Id,
                Label = student.ClassName + " · " + student.FullName
            })
            .ToListAsync();

        ViewBag.Students = new SelectList(students, "Id", "Label");
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
