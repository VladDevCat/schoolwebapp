namespace swa.Models;

public class SchoolTaskDashboardViewModel
{
    public IReadOnlyList<SchoolTask> Tasks { get; set; } = [];
    public string? Search { get; set; }
    public string? Subject { get; set; }
    public SchoolTaskStatus? Status { get; set; }
    public IReadOnlyList<string> Subjects { get; set; } = [];
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public int DueSoonCount { get; set; }
    public int OverdueCount { get; set; }
}
