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

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { username, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty login response.");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return login;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/tasks", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<TaskDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/tasks/{taskId}/complete");
        var response = await _httpClient.SendAsync(request, cancellationToken);
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
        string Username,
        string Role,
        Guid TenantId,
        string TenantName);

    public record TaskDto(
        Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTime CreatedDate,
        DateTime? CompletedAt,
        Guid AssignedUserId,
        string AssignedUsername);

    private record ErrorResponse(string Error);
}
