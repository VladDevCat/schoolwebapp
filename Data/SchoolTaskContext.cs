using Microsoft.EntityFrameworkCore;
using swa.Models;

namespace swa.Data;

public class SchoolTaskContext : DbContext
{
    public SchoolTaskContext(DbContextOptions<SchoolTaskContext> options) : base(options)
    {
    }

    public DbSet<SchoolTask> SchoolTasks => Set<SchoolTask>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<TeacherAccount> TeacherAccounts => Set<TeacherAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>()
            .HasIndex(student => new { student.FullName, student.ClassName })
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(student => student.Login)
            .IsUnique();

        modelBuilder.Entity<TeacherAccount>()
            .HasIndex(account => account.Login)
            .IsUnique();

        modelBuilder.Entity<SchoolTask>()
            .HasOne(task => task.AssignedStudent)
            .WithMany(student => student.Tasks)
            .HasForeignKey(task => task.AssignedStudentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
