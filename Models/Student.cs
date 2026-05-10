using System.ComponentModel.DataAnnotations;

namespace swa.Models;

public class Student
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите ФИО ученика")]
    [StringLength(140, ErrorMessage = "ФИО не должно быть длиннее 140 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите класс")]
    [StringLength(20, ErrorMessage = "Класс не должен быть длиннее 20 символов")]
    [Display(Name = "Класс")]
    public string ClassName { get; set; } = string.Empty;

    [StringLength(120)]
    [EmailAddress(ErrorMessage = "Укажите корректный email")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(80)]
    [Display(Name = "Логин")]
    public string? Login { get; set; }

    [StringLength(128)]
    public string? PasswordHash { get; set; }

    [Display(Name = "Добавлен")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SchoolTask> Tasks { get; set; } = [];
}
