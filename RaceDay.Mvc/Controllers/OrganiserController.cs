using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize("Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(RaceDayApiClient api) : base(api) { }

    private int CurrentUserId => HttpContext.Session.GetInt32("UserId") ?? 0;

    /// <summary>GET api/events is public and returns everything, so keep only this organiser's events.</summary>
    private async Task<(List<EventResponse>? Events, HttpResponseMessage Response)> LoadMyEventsAsync()
    {
        var (all, response) = await Api.GetAsync<List<EventResponse>>(ApiRoutes.Events);
        return (all?.Where(e => e.OrganiserId == CurrentUserId).OrderBy(e => e.EventDate).ToList(), response);
    }

    // ---------------------------------------------------------------- Dashboard
    public async Task<IActionResult> Index()
    {
        var (events, response) = await LoadMyEventsAsync();
        if (CheckExpired(response) is { } expired) return expired;
        if (events == null) TempData["Error"] = await ErrorOf(response);
        return View(new OrganiserDashboardVm { Events = events ?? new() });
    }

    // ---------------------------------------------------------------- Events CRUD
    public async Task<IActionResult> Events()
    {
        var (events, response) = await LoadMyEventsAsync();
        if (CheckExpired(response) is { } expired) return expired;
        if (events == null) TempData["Error"] = await ErrorOf(response);
        return View(events ?? new());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Events, vm.ToRequest());
        if (CheckExpired(response) is { } expired) return expired;
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ErrorOf(response));
            return View(vm);
        }

        string message = "Event created. Now add its categories.";
        var created = await Api.ReadAsync<EventResponse>(response);

        if (vm.Banner is { Length: > 0 } && created != null)
        {
            var up = await Api.UploadFileAsync(ApiRoutes.EventBanner(created.EventId), vm.Banner);
            message = up.IsSuccessStatusCode
                ? "Event created and banner uploaded. Now add its categories."
                : "Event created, but the banner upload failed: " + await ErrorOf(up);
        }

        TempData["Success"] = message;
        return created != null
            ? RedirectToAction(nameof(Categories), new { id = created.EventId })
            : RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var (ev, response) = await Api.GetAsync<EventResponse>(ApiRoutes.Event(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (ev == null || ev.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = ev == null ? await ErrorOf(response) : "You can only change events that you created.";
            return RedirectToAction(nameof(Events));
        }

        return View(new EventFormVm
        {
            EventId = ev.EventId, Title = ev.Title, Description = ev.Description, EventDate = ev.EventDate,
            Location = ev.Location, DistanceKM = ev.DistanceKM, EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await Api.SendAsync(HttpMethod.Put, ApiRoutes.Event(vm.EventId), vm.ToRequest());
        if (CheckExpired(response) is { } expired) return expired;
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await ErrorOf(response));
            return View(vm);
        }

        string message = "Event updated.";
        if (vm.Banner is { Length: > 0 })
        {
            var up = await Api.UploadFileAsync(ApiRoutes.EventBanner(vm.EventId), vm.Banner);
            message = up.IsSuccessStatusCode
                ? "Event updated and banner uploaded."
                : "Event updated, but the banner upload failed: " + await ErrorOf(up);
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var response = await Api.SendAsync(HttpMethod.Delete, ApiRoutes.Event(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (response.IsSuccessStatusCode) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = await ErrorOf(response);
        return RedirectToAction(nameof(Events));
    }

    // ---------------------------------------------------------------- Categories
    public async Task<IActionResult> Categories(int id)
    {
        var (ev, response) = await Api.GetAsync<EventResponse>(ApiRoutes.Event(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (ev == null)
        {
            TempData["Error"] = await ErrorOf(response);
            return RedirectToAction(nameof(Events));
        }
        return View(new CategoriesVm { Event = ev });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int id, CategoryFormVm model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please enter a category name, a distance above 0 and a fee of 0 or more.";
            return RedirectToAction(nameof(Categories), new { id });
        }

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Categories(id),
            new CategoryRequest { CategoryName = model.CategoryName, DistanceKM = model.DistanceKM, Fee = model.Fee });
        if (CheckExpired(response) is { } expired) return expired;
        if (response.IsSuccessStatusCode) TempData["Success"] = "Category added.";
        else TempData["Error"] = await ErrorOf(response);
        return RedirectToAction(nameof(Categories), new { id });
    }

    // ---------------------------------------------------------------- Enrolments
    public async Task<IActionResult> Enrolments(int? eventId)
    {
        var (events, response) = await LoadMyEventsAsync();
        if (CheckExpired(response) is { } expired) return expired;

        var vm = new EnrolmentsPageVm { Events = events ?? new(), SelectedEventId = eventId };
        if (eventId.HasValue)
        {
            var (items, itemsResponse) = await Api.GetAsync<List<EnrolmentResponse>>(ApiRoutes.EventEnrolments(eventId.Value));
            if (items == null) TempData["Error"] = await ErrorOf(itemsResponse);
            vm.Items = items ?? new();
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEnrolment(int id, int eventId)
    {
        var response = await Api.SendAsync(HttpMethod.Put, ApiRoutes.ConfirmEnrolment(id));
        if (CheckExpired(response) is { } expired) return expired;
        if (response.IsSuccessStatusCode) TempData["Success"] = "Enrolment confirmed.";
        else TempData["Error"] = await ErrorOf(response);
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------------------------------------------------------------- Results
    public async Task<IActionResult> Results(int? eventId)
    {
        var (events, response) = await LoadMyEventsAsync();
        if (CheckExpired(response) is { } expired) return expired;

        var vm = new ResultsPageVm { Events = events ?? new(), SelectedEventId = eventId };
        if (eventId.HasValue)
        {
            vm.SelectedEvent = vm.Events.FirstOrDefault(e => e.EventId == eventId.Value);
            vm.EventHasPassed = vm.SelectedEvent != null && vm.SelectedEvent.EventDate <= DateTime.Now;

            var (enrolments, enrolResponse) = await Api.GetAsync<List<EnrolmentResponse>>(ApiRoutes.EventEnrolments(eventId.Value));
            if (enrolments == null) TempData["Error"] = await ErrorOf(enrolResponse);
            var (results, _) = await Api.GetAsync<List<ResultResponse>>(ApiRoutes.EventResults(eventId.Value));

            vm.Results = results ?? new();
            var done = vm.Results.Select(r => r.EnrolmentId).ToHashSet();
            // Only active enrolments without a result can have a result captured.
            vm.Pending = (enrolments ?? new())
                .Where(e => !done.Contains(e.EnrolmentId) &&
                            !string.Equals(e.PaymentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResult(CaptureResultVm model)
    {
        if (!TimeSpan.TryParse(model.FinishTime, out var time) || time <= TimeSpan.Zero ||
            model.Position <= 0 || model.EnrolmentId <= 0)
        {
            TempData["Error"] = "Enter a finish time as hh:mm:ss and a finishing position of 1 or more.";
            return RedirectToAction(nameof(Results), new { eventId = model.EventId });
        }

        var response = await Api.SendAsync(HttpMethod.Post, ApiRoutes.Results, new ResultRequest
        {
            EnrolmentId = model.EnrolmentId,
            FinishTimeSeconds = (int)time.TotalSeconds,
            Position = model.Position
        });
        if (CheckExpired(response) is { } expired) return expired;
        if (response.IsSuccessStatusCode) TempData["Success"] = "Result captured.";
        else TempData["Error"] = await ErrorOf(response);
        return RedirectToAction(nameof(Results), new { eventId = model.EventId });
    }
}
