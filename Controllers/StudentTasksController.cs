using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using swa.Data;
using swa.Models;

namespace swa.Controllers;

[Authorize(Roles = "Student")]
public class StudentTasksController : Controller
{
    private readonly SchoolTaskContext _context;

    public StudentTasksController(SchoolTaskContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tasks = await _context.SchoolTasks
            .AsNoTracking()
            .Where(task => task.AssignedStudentId == studentId)
            .OrderBy(task => task.Status == SchoolTaskStatus.Completed)
            .ThenBy(task => task.DueDate)
            .ThenByDescending(task => task.Priority)
            .ToListAsync();

        return View(tasks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await _context.SchoolTasks
            .FirstOrDefaultAsync(item => item.Id == id && item.AssignedStudentId == studentId);

        if (task is null)
        {
            return NotFound();
        }

        task.Status = SchoolTaskStatus.Completed;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
