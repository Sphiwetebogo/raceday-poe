using Microsoft.AspNetCore.Mvc; 
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Participant")]
public class ParticipantController : BaseController
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };

    public ParticipantController(RaceDayApiClient api) : base(api) { }

    // "My Athlete Profile" dashboard: enrolment totals and a table of the participant's enrolments.
    public async Task<IActionResult> Index()
    {
        var (items, response) = await Api.GetAsync<List<EnrolmentResponse>>(ApiRoutes.MyEnrolments);
        if (CheckExpired(response) is { } expired) return expired;
        if (items == null) TempData["Error"] = await ErrorOf(response);

        // Fees are not part of the enrolment response, so look them up from the events' categories.
        var (events, _) = await Api.GetAsync<List<EventResponse>>(ApiRoutes.Events);
        var fees = (events ?? new()).SelectMany(e => e.Categories)
            .GroupBy(c => c.CategoryId).ToDictionary(g => g.Key, g => g.First().Fee);

        var vm = new ParticipantDashboardVm
        {
            Rows = (items ?? new()).OrderBy(i => i.EventDate).Select(i => new EnrolmentRowVm
            {
                Enrolment = i,
                Fee = fees.TryGetValue(i.CategoryId, out var f) ? f : null
            }).ToList()
        };
        return View(vm);
    }

    // Explore Events - filtering is done by the API (search, eventType, upcomingOnly).
    public async Task<IActionResult> Explore(string? type, string? search)
    {
        var query = new List<string> { "upcomingOnly=true" };
        if (!string.IsNullOrWhiteSpace(type) && EventTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            query.Add("eventType=" + Uri.EscapeDataString(type));
        if (!string.IsNullOrWhiteSpace(search))
            query.Add("search=" + Uri.EscapeDataString(search.Trim()));

        var (events, response) = await Api.GetAsync<List<EventResponse>>(ApiRoutes.Events + "?" + string.Join("&", query));
        if (CheckExpired(response) is { } expired) return expired;
        if (events == null) TempData["Error"] = await ErrorOf(response);

        ViewBag.Type = type;
        ViewBag.Search = search;
        return View(events ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category before entering.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Enrolments, new EnrolRequest { CategoryId = categoryId });
        if (CheckExpired(response) is { } expired) return expired;

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = await ErrorOf(response);
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        TempData["Success"] = "You have entered the event successfully.";
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyEvents()
    {
        var (items, response) = await Api.GetAsync<List<EnrolmentResponse>>(ApiRoutes.MyEnrolments);
        if (CheckExpired(response) is { } expired) return expired;
        if (items == null) TempData["Error"] = await ErrorOf(response);
        return View((items ?? new()).OrderBy(i => i.EventDate).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var response = await Api.SendAsync(HttpMethod.Delete, ApiRoutes.Enrolment(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (response.IsSuccessStatusCode) TempData["Success"] = "Your enrolment was cancelled.";
        else TempData["Error"] = await ErrorOf(response);
        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var (items, response) = await Api.GetAsync<List<ResultResponse>>(ApiRoutes.MyResults);
        if (CheckExpired(response) is { } expired) return expired;
        if (items == null) TempData["Error"] = await ErrorOf(response);
        return View(items ?? new());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var (user, response) = await Api.GetAsync<UserResponse>(ApiRoutes.Profile);
        if (CheckExpired(response) is { } expired) return expired;
        if (user == null)
        {
            TempData["Error"] = await ErrorOf(response);
            return RedirectToAction(nameof(Index));
        }
        return View(new ProfileVm { FullName = user.FullName, Email = user.Email, ProfileImageUrl = user.ProfileImageUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Put, ApiRoutes.Profile,
            new UpdateProfileRequest { FullName = vm.FullName, Email = vm.Email });
        if (CheckExpired(response) is { } expired) return expired;
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ErrorOf(response));
            return View(vm);
        }

        string message = "Profile updated.";
        if (vm.ProfileImage is { Length: > 0 })
        {
            var up = await Api.UploadFileAsync(ApiRoutes.ProfileImage, vm.ProfileImage);
            message = up.IsSuccessStatusCode
                ? "Profile and picture updated."
                : "Profile updated, but the picture upload failed: " + await ErrorOf(up);
        }

        HttpContext.Session.SetString("DisplayName", vm.FullName.Split(' ')[0]);
        HttpContext.Session.SetString("Email", vm.Email);
        TempData["Success"] = message;
        return RedirectToAction(nameof(Profile));
    }
}
