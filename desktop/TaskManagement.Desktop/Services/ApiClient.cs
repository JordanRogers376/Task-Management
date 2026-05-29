using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.Desktop.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private string? _token;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty login response.");

        _token = login.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return login;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<TaskDto>>(JsonOptions, cancellationToken)
               ?? [];
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/tasks/{taskId}/complete", null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TaskDto>(JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException("Empty task response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        string message;
        try
        {
            var error = JsonSerializer.Deserialize<ErrorResponse>(body, JsonOptions);
            message = error?.Error ?? response.ReasonPhrase ?? "Request failed";
        }
        catch
        {
            message = response.ReasonPhrase ?? "Request failed";
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }

    public record LoginResponse(
        string Token,
        DateTime ExpiresAt,
        string Email,
        string Role,
        Guid TenantId,
        string TenantName);

    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        string CreatedByEmail);

    private record ErrorResponse(string Error);
}
