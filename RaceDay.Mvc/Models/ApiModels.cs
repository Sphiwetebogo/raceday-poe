namespace RaceDay.Mvc.Models;

// DTOs that mirror the RaceDay.Api (Part 2) request/response classes. JSON matching is case-insensitive.
// Enums (EventType, PaymentStatus) travel as strings because the API uses JsonStringEnumConverter.

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal Fee { get; set; }
}

public class CategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public decimal Fee { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string EventType { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public List<CategoryResponse> Categories { get; set; } = new();
}

public class EventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public string EventType { get; set; } = "Run";
}

public class EnrolRequest
{
    public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class ResultRequest
{
    public int EnrolmentId { get; set; }
    public int FinishTimeSeconds { get; set; }
    public int Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int FinishTimeSeconds { get; set; }
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
    public int TotalFinishers { get; set; }
}
