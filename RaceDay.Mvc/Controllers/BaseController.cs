using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public abstract class BaseController : Controller
{
    protected readonly RaceDayApiClient Api;

    protected BaseController(RaceDayApiClient api) => Api = api;

    /// <summary>If the API says 401 (its session expired), clear MVC session and send the user to Login.</summary>
    protected IActionResult? CheckExpired(HttpResponseMessage response)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return null;
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Auth");
    }

    protected async Task<string> ErrorOf(HttpResponseMessage response) => await Api.ReadErrorAsync(response);
}
