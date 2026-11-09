using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize]
public class EventsController : BaseController
{
    public EventsController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Details(int id)
    {
        // The event response already contains its categories.
        var (ev, response) = await Api.GetAsync<EventResponse>(ApiRoutes.Event(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (ev == null)
        {
            TempData["Error"] = await ErrorOf(response);
            return IsOrganiser() ? RedirectToAction("Events", "Organiser") : RedirectToAction("Explore", "Participant");
        }
        return View(ev);
    }

    private bool IsOrganiser() =>
        string.Equals(HttpContext.Session.GetString("Role"), "Organiser", StringComparison.OrdinalIgnoreCase);
}
