using System.ComponentModel.DataAnnotations;

namespace swa.Models;

public class TeacherAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Login { get; set; } = string.Empty;

    [Required]
    [StringLength(140)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
