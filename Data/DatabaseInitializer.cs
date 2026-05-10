using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using swa.Models;

namespace swa.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(SchoolTaskContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await EnsureColumnsAsync(context);
        await SeedAsync(context);
    }

    private static async Task EnsureColumnsAsync(SchoolTaskContext context)
    {
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS "Students" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
                "FullName" TEXT NOT NULL,
                "ClassName" TEXT NOT NULL,
                "Email" TEXT NULL,
                "Login" TEXT NULL,
                "PasswordHash" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        await ExecuteAsync(connection, """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_FullName_ClassName"
            ON "Students" ("FullName", "ClassName");
            """);

        await EnsureColumnAsync(connection, "Students", "Login", "TEXT NULL");
        await EnsureColumnAsync(connection, "Students", "PasswordHash", "TEXT NULL");

        await ExecuteAsync(connection, """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_Login"
            ON "Students" ("Login");
            """);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS "TeacherAccounts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TeacherAccounts" PRIMARY KEY AUTOINCREMENT,
                "Login" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        await ExecuteAsync(connection, """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeacherAccounts_Login"
            ON "TeacherAccounts" ("Login");
            """);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('SchoolTasks');";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }
        }

        if (!columns.Contains("AssignedStudentId"))
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE SchoolTasks ADD COLUMN AssignedStudentId INTEGER NULL;";
            await alterCommand.ExecuteNonQueryAsync();
        }
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, string definition)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{table}');";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(1));
            }
        }

        if (!columns.Contains(column))
        {
            await using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
            await alterCommand.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SeedAsync(SchoolTaskContext context)
    {
        if (!await context.TeacherAccounts.AnyAsync())
        {
            context.TeacherAccounts.Add(new TeacherAccount
            {
                Login = "teacher",
                DisplayName = "Учитель",
                PasswordHash = PasswordHasher.Hash("teacher123")
            });
        }

        if (!await context.SchoolTasks.AnyAsync())
        {
            context.SchoolTasks.AddRange(
                new SchoolTask
                {
                    Title = "Подготовить презентацию по истории",
                    Description = "Собрать 8-10 слайдов о реформах Петра I и добавить список источников.",
                    Subject = "История",
                    Teacher = "Иванова Н. П.",
                    DueDate = DateTime.Today.AddDays(2),
                    Priority = TaskPriority.High,
                    Status = SchoolTaskStatus.InProgress
                },
                new SchoolTask
                {
                    Title = "Решить задачи по алгебре",
                    Description = "Параграф 24, номера 412-428.",
                    Subject = "Алгебра",
                    Teacher = "Смирнов А. В.",
                    DueDate = DateTime.Today.AddDays(1),
                    Priority = TaskPriority.Medium,
                    Status = SchoolTaskStatus.Planned
                });
        }

        if (!await context.Students.AnyAsync())
        {
            var demoStudent = new Student
            {
                FullName = "Иван Петров",
                ClassName = "8А",
                Email = "ivan.petrov@example.local",
                Login = "student",
                PasswordHash = PasswordHasher.Hash("student123")
            };

            context.Students.Add(demoStudent);
            context.SchoolTasks.Add(new SchoolTask
            {
                Title = "Прочитать параграф по биологии",
                Description = "Подготовить краткий конспект и выписать новые термины.",
                Subject = "Биология",
                Teacher = "Классный руководитель",
                DueDate = DateTime.Today.AddDays(3),
                Priority = TaskPriority.Medium,
                Status = SchoolTaskStatus.Planned,
                AssignedStudent = demoStudent
            });
        }

        var studentsWithoutLogin = await context.Students
            .Where(student => string.IsNullOrWhiteSpace(student.Login) || string.IsNullOrWhiteSpace(student.PasswordHash))
            .ToListAsync();

        foreach (var student in studentsWithoutLogin)
        {
            student.Login = string.IsNullOrWhiteSpace(student.Login)
                ? await BuildUniqueStudentLoginAsync(context, student)
                : student.Login;
            student.PasswordHash = string.IsNullOrWhiteSpace(student.PasswordHash)
                ? PasswordHasher.Hash("student123")
                : student.PasswordHash;
        }

        await context.SaveChangesAsync();
    }

    private static async Task<string> BuildUniqueStudentLoginAsync(SchoolTaskContext context, Student student)
    {
        var baseLogin = NormalizeLogin(student.ClassName + "_" + student.FullName);
        var login = baseLogin;
        var suffix = 1;

        while (await context.Students.AnyAsync(item => item.Id != student.Id && item.Login == login))
        {
            suffix++;
            login = $"{baseLogin}{suffix}";
        }

        return login;
    }

    private static string NormalizeLogin(string value)
    {
        var result = new System.Text.StringBuilder();

        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                result.Append(ch);
            }
            else if (result.Length > 0 && result[^1] != '.')
            {
                result.Append('.');
            }
        }

        return result.ToString().Trim('.');
    }
}
