using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.ViewModels;

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one letter and one number.")]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password), ErrorMessage = "The passwords do not match."), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>1 = Organiser, 2 = Participant (same ids as the API).</summary>
    [Range(1, 2)]
    public int RoleId { get; set; } = 2;

    [Display(Name = "Organiser code")]
    public string? OrganiserCode { get; set; }
}

public class OrganiserDashboardVm
{
    public List<EventResponse> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int UpcomingEvents => Events.Count(e => e.EventDate >= DateTime.Now);
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
}

public class EventFormVm
{
    public int EventId { get; set; }

    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(4000)] public string? Description { get; set; }

    [Required, DataType(DataType.Date), Display(Name = "Date")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

    [Required, StringLength(150)] public string Location { get; set; } = string.Empty;

    [Range(0.1, 999.99), Display(Name = "Distance (km)")]
    public decimal DistanceKM { get; set; }

    [Required, Display(Name = "Event type")] public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }
    public IFormFile? Banner { get; set; }

    public EventRequest ToRequest() => new()
    {
        Title = Title, Description = Description, EventDate = EventDate,
        Location = Location, DistanceKM = DistanceKM, EventType = EventType
    };
}

public class CategoryFormVm
{
    [Required, StringLength(100), Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99), Display(Name = "Distance (km)")]
    public decimal DistanceKM { get; set; }

    [Range(0, 99999999.99)]
    public decimal Fee { get; set; }
}

public class CategoriesVm
{
    public EventResponse Event { get; set; } = new();
    public CategoryFormVm NewCategory { get; set; } = new();
}

public class EnrolmentsPageVm
{
    public List<EventResponse> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public List<EnrolmentResponse> Items { get; set; } = new();
}

public class ResultsPageVm
{
    public List<EventResponse> Events { get; set; } = new();
    public int? SelectedEventId { get; set; }
    public EventResponse? SelectedEvent { get; set; }
    public bool EventHasPassed { get; set; }
    public List<EnrolmentResponse> Pending { get; set; } = new();
    public List<ResultResponse> Results { get; set; } = new();
}

public class CaptureResultVm
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    /// <summary>hh:mm:ss, converted to seconds before it is sent to the API.</summary>
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class ProfileVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }
    public IFormFile? ProfileImage { get; set; }
}

public class EnrolmentRowVm
{
    public EnrolmentResponse Enrolment { get; set; } = new();
    public decimal? Fee { get; set; }
}

public class ParticipantDashboardVm
{
    public List<EnrolmentRowVm> Rows { get; set; } = new();
    private IEnumerable<EnrolmentRowVm> Active => Rows.Where(r =>
        !string.Equals(r.Enrolment.PaymentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase));
    public int Total => Active.Count();
    public int Confirmed => Active.Count(r => string.Equals(r.Enrolment.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));
    public int Pending => Active.Count(r => string.Equals(r.Enrolment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase));
}
