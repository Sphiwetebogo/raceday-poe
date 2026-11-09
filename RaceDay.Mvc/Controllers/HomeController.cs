using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController : BaseController
{
    public HomeController(RaceDayApiClient api) : base(api) { }

    public async Task<IActionResult> Index()
    {
        // GET api/events is public, so the landing page can show upcoming events to anyone.
        var (events, _) = await Api.GetAsync<List<EventResponse>>(ApiRoutes.Events + "?upcomingOnly=true");
        return View((events ?? new()).OrderBy(e => e.EventDate).Take(3).ToList());
    }

    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
