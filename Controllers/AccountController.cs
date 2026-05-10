using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using swa.Data;
using swa.Models;

namespace swa.Controllers;

public class AccountController : Controller
{
    private readonly SchoolTaskContext _context;

    public AccountController(SchoolTaskContext context)
    {
        _context = context;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var passwordHash = PasswordHasher.Hash(model.Password);
        var account = await _context.TeacherAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Login == model.Login && item.PasswordHash == passwordHash);

        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.DisplayName),
            new(ClaimTypes.Role, "Teacher")
        };

        await SignInAsync(claims);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Teacher");
    }

    public IActionResult StudentLogin(string? returnUrl = null)
    {
        return View(new StudentLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StudentLogin(StudentLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var passwordHash = PasswordHasher.Hash(model.Password);
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Login == model.Login && item.PasswordHash == passwordHash);

        if (student is null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, student.Id.ToString()),
            new(ClaimTypes.Name, student.FullName),
            new(ClaimTypes.Role, "Student"),
            new("ClassName", student.ClassName)
        };

        await SignInAsync(claims);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "StudentTasks");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("SchoolCookie");
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInAsync(IReadOnlyCollection<Claim> claims)
    {
        await HttpContext.SignInAsync(
            "SchoolCookie",
            new ClaimsPrincipal(new ClaimsIdentity(claims, "SchoolCookie")));
    }
}
