namespace RaceDay.Mvc.Services;

/// <summary>Every RaceDay.Api route used by the MVC app (taken from the Part 2 controllers).</summary>
public static class ApiRoutes
{
    public const string Register = "api/auth/register";
    public const string Login = "api/auth/login";
    public const string Logout = "api/auth/logout";

    public const string Events = "api/events";                                   // GET (search, eventType, upcomingOnly), POST
    public static string Event(int id) => $"api/events/{id}";                    // GET, PUT, DELETE
    public static string EventBanner(int id) => $"api/events/{id}/banner";       // POST multipart (Part 3)
    public static string Categories(int eventId) => $"api/events/{eventId}/categories";   // GET, POST
    public static string EventEnrolments(int eventId) => $"api/events/{eventId}/enrolments"; // GET (organiser)
    public static string EventResults(int eventId) => $"api/events/{eventId}/results";       // GET (public)

    public const string Enrolments = "api/enrolments";                           // POST
    public const string MyEnrolments = "api/enrolments/my";                      // GET
    public static string Enrolment(int id) => $"api/enrolments/{id}";            // DELETE (cancel)
    public static string ConfirmEnrolment(int id) => $"api/enrolments/{id}/confirm"; // PUT (organiser)

    public const string Results = "api/results";                                 // POST
    public const string MyResults = "api/results/my";                            // GET

    public const string Profile = "api/users/profile";                           // GET, PUT
    public const string ProfileImage = "api/users/profile/image";                // POST multipart (Part 3)
}
