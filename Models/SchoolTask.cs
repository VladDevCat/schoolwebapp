using System.ComponentModel.DataAnnotations;

namespace swa.Models;

public enum TaskPriority
{
    [Display(Name = "Низкий")]
    Low = 0,
    [Display(Name = "Средний")]
    Medium = 1,
    [Display(Name = "Высокий")]
    High = 2
}

public enum SchoolTaskStatus
{
    [Display(Name = "Запланировано")]
    Planned = 0,
    [Display(Name = "В работе")]
    InProgress = 1,
    [Display(Name = "Готово")]
    Completed = 2
}

public class SchoolTask
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название задачи")]
    [StringLength(120, ErrorMessage = "Название не должно быть длиннее 120 символов")]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [StringLength(800, ErrorMessage = "Описание не должно быть длиннее 800 символов")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Укажите предмет")]
    [StringLength(80, ErrorMessage = "Предмет не должен быть длиннее 80 символов")]
    [Display(Name = "Предмет")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Имя преподавателя не должно быть длиннее 80 символов")]
    [Display(Name = "Преподаватель")]
    public string? Teacher { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Срок")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Приоритет")]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Display(Name = "Статус")]
    public SchoolTaskStatus Status { get; set; } = SchoolTaskStatus.Planned;

    [Display(Name = "Ученик")]
    public int? AssignedStudentId { get; set; }

    public Student? AssignedStudent { get; set; }

    [Display(Name = "Создано")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
