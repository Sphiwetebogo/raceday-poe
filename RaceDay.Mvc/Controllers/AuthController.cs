using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class AuthController : BaseController
{
    public AuthController(RaceDayApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (vm.RoleId == 1 && string.IsNullOrWhiteSpace(vm.OrganiserCode))
            ModelState.AddModelError(nameof(vm.OrganiserCode), "The organiser code is required to register as an Organiser.");

        if (!ModelState.IsValid) return View(vm);

        var request = new RegisterRequest
        {
            FullName = vm.FullName, Email = vm.Email, Password = vm.Password,
            RoleId = vm.RoleId, OrganiserCode = vm.RoleId == 1 ? vm.OrganiserCode : null
        };

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Register, request);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ErrorOf(response));
            return View(vm);
        }

        TempData["Success"] = "Registration successful. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Login,
            new LoginRequest { Email = vm.Email, Password = vm.Password });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                ? await ErrorOf(response)
                : "Invalid email or password.");
            return View(vm);
        }

        var user = await Api.ReadAsync<UserResponse>(response);
        string? apiCookie = RaceDayApiClient.ExtractSessionCookie(response);
        if (user == null || string.IsNullOrWhiteSpace(apiCookie))
        {
            ModelState.AddModelError("", "Login could not be completed. Please try again.");
            return View(vm);
        }

        // MVC session: who is logged in. API session cookie: proves identity to protected API endpoints.
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("ApiSessionCookie", apiCookie);
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        HttpContext.Session.SetString("DisplayName", user.FullName.Split(' ')[0]);

        return user.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await Api.SendAsync(HttpMethod.Post, ApiRoutes.Logout);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
