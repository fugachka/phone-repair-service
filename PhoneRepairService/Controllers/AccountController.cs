using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneRepairService.Data;
using PhoneRepairService.Models;
using PhoneRepairService.Services;
using PhoneRepairService.ViewModels;

namespace PhoneRepairService.Controllers;

/// <summary>Т.10 «Авторизоваться», выход из системы.</summary>
public class AccountController : Controller
{
    private readonly AuthService _auth;

    public AccountController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var user = _auth.Authenticate(model.Login ?? "", model.Password ?? "");
        if (user == null)
        {
            model.Password = null;
            ModelState.Clear();
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}

/// <summary>Стартовая страница: переход к функциям роли пользователя.</summary>
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Account");
        if (User.IsInRole(Roles.Client)) return RedirectToAction("Index", "MyOrders");
        if (User.IsInRole(Roles.Master)) return RedirectToAction("Index", "WorkOrders");
        if (User.IsInRole(Roles.Admin)) return RedirectToAction("Index", "Orders");
        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}

/// <summary>Общие действия контроллеров: текущий пользователь, сообщения.</summary>
public abstract class AppController : Controller
{
    protected int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    protected T? CurrentUser<T>(RepairDb db) where T : User => db.GetUser(CurrentUserId) as T;

    protected void Flash(string message) => TempData["Message"] = message;

    protected void AddError(BusinessRuleException ex) =>
        ModelState.AddModelError(ex.Field ?? "", ex.Message);
}
