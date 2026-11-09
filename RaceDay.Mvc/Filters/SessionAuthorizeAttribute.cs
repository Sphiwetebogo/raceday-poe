using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Session-based page protection (no extra auth framework).
/// Not logged in  -> redirect to Login.
/// Wrong role     -> redirect to Access Denied.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.GetInt32("UserId").HasValue)
        {
            if (context.Controller is Controller c)
                c.TempData["Error"] = "Please log in to continue.";
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        if (_role != null &&
            !string.Equals(session.GetString("Role"), _role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
        }
    }
}
