using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

/// <summary>
/// The ONLY class that talks to the Part 2 API. It forwards the API session cookie that is
/// kept inside the MVC session, so protected API endpoints recognise the logged-in user.
/// </summary>
public class RaceDayApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Name of the session cookie the Part 2 API sets (SessionKeys.CookieName in RaceDay.Api).</summary>
    public const string ApiSessionCookieName = ".RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RaceDayApiClient(IHttpClientFactory factory, IHttpContextAccessor accessor)
    {
        _factory = factory;
        _httpContextAccessor = accessor;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        string? apiCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(apiCookie))
            request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
        return request;
    }

    private static HttpResponseMessage Unavailable() => new(HttpStatusCode.ServiceUnavailable)
    {
        Content = new StringContent("The RaceDay service is currently unavailable. Please try again shortly.")
    };

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        try
        {
            var client = _factory.CreateClient("RaceDayApi");
            using var request = CreateRequest(method, url);
            if (body != null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unavailable();
        }
    }

    /// <summary>GET and deserialise. Data is null when the call fails.</summary>
    public async Task<(T? Data, HttpResponseMessage Response)> GetAsync<T>(string url)
    {
        var response = await SendAsync(HttpMethod.Get, url);
        return (await ReadAsync<T>(response), response);
    }

    /// <summary>Send a file (multipart/form-data) to the API. The API - not MVC - uploads it to Azure.</summary>
    public async Task<HttpResponseMessage> UploadFileAsync(string url, IFormFile file)
    {
        try
        {
            var client = _factory.CreateClient("RaceDayApi");
            using var request = CreateRequest(HttpMethod.Post, url);
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(fileContent, "file", file.FileName);
            request.Content = form;
            return await client.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unavailable();
        }
    }

    public async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) return default;
        try
        {
            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>Turns a failed API response into a short, user-friendly message (never a stack trace).</summary>
    public async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return "Your session has expired. Please log in again.";
            case HttpStatusCode.Forbidden:
                return "You do not have permission to do that.";
            case HttpStatusCode.NotFound:
                return "The requested item could not be found.";
        }

        string body = string.Empty;
        try { body = (await response.Content.ReadAsStringAsync()).Trim(); } catch { /* ignore */ }

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable && body.Length is > 0 and < 200)
            return body;

        if (body.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);

                // ASP.NET validation problem: { "errors": { "Field": ["message", ...] } }
                if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                            ? p.Value.EnumerateArray().Select(m => m.GetString()).ToList()
                            : new List<string?>())
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Take(3)
                        .ToList();
                    string joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }

                foreach (var key in new[] { "detail", "message", "error", "title" })
                {
                    if (doc.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                        return el.GetString() ?? Generic(response);
                }
            }
            catch (JsonException) { /* fall through */ }
            return Generic(response);
        }

        if (body.Length is > 0 and <= 200 && !body.StartsWith('<'))
            return body.Trim('"');

        return Generic(response);
    }

    private static string Generic(HttpResponseMessage r) =>
        $"The request could not be completed ({(int)r.StatusCode}). Please check your input and try again.";

    /// <summary>Extracts "name=value" of the API session cookie from a login response.</summary>
    public static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;
        string? header = values.FirstOrDefault(v =>
            v.StartsWith(ApiSessionCookieName + "=", StringComparison.OrdinalIgnoreCase));
        return header?.Split(';', 2)[0];
    }
}
