namespace swa.Models;

public class TeacherDashboardViewModel
{
    public IReadOnlyList<SchoolTask> UrgentTasks { get; set; } = [];
    public IReadOnlyList<SchoolTask> RecentTasks { get; set; } = [];
    public IReadOnlyList<TeacherWorkloadItem> SubjectWorkload { get; set; } = [];
    public IReadOnlyList<TeacherWorkloadItem> TeacherWorkload { get; set; } = [];
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int HighPriorityCount { get; set; }
}

public class TeacherWorkloadItem
{
    public string Name { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
}
