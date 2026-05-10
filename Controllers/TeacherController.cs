using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using swa.Data;
using swa.Models;

namespace swa.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly SchoolTaskContext _context;

    public TeacherController(SchoolTaskContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tasks = await _context.SchoolTasks
            .Include(task => task.AssignedStudent)
            .AsNoTracking()
            .ToListAsync();

        var viewModel = new TeacherDashboardViewModel
        {
            TotalCount = tasks.Count,
            ActiveCount = tasks.Count(task => task.Status != SchoolTaskStatus.Completed),
            CompletedCount = tasks.Count(task => task.Status == SchoolTaskStatus.Completed),
            HighPriorityCount = tasks.Count(task => task.Priority == TaskPriority.High && task.Status != SchoolTaskStatus.Completed),
            UrgentTasks = tasks
                .Where(task => task.Status != SchoolTaskStatus.Completed && task.DueDate <= today.AddDays(3))
                .OrderBy(task => task.DueDate)
                .ThenByDescending(task => task.Priority)
                .ToList(),
            RecentTasks = tasks
                .OrderByDescending(task => task.CreatedAt)
                .Take(6)
                .ToList(),
            SubjectWorkload = tasks
                .GroupBy(task => task.Subject)
                .Select(group => new TeacherWorkloadItem
                {
                    Name = group.Key,
                    TotalCount = group.Count(),
                    ActiveCount = group.Count(task => task.Status != SchoolTaskStatus.Completed),
                    CompletedCount = group.Count(task => task.Status == SchoolTaskStatus.Completed)
                })
                .OrderByDescending(item => item.ActiveCount)
                .ThenBy(item => item.Name)
                .ToList(),
            TeacherWorkload = tasks
                .Where(task => !string.IsNullOrWhiteSpace(task.Teacher))
                .GroupBy(task => task.Teacher!)
                .Select(group => new TeacherWorkloadItem
                {
                    Name = group.Key,
                    TotalCount = group.Count(),
                    ActiveCount = group.Count(task => task.Status != SchoolTaskStatus.Completed),
                    CompletedCount = group.Count(task => task.Status == SchoolTaskStatus.Completed)
                })
                .OrderByDescending(item => item.ActiveCount)
                .ThenBy(item => item.Name)
                .ToList()
        };

        return View(viewModel);
    }
}
