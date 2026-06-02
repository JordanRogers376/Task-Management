using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private ApiClient _apiClient;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _username = "admin@acme.com";

    [ObservableProperty]
    private string _password = "Password123!";

    [ObservableProperty]
    private string _loginError = string.Empty;

    [ObservableProperty]
    private string _tenantName = string.Empty;

    [ObservableProperty]
    private string _userInfo = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private ApiClient.TaskDto? _selectedTask;

    public ObservableCollection<ApiClient.TaskDto> Tasks { get; } = new();

    public MainViewModel() => _apiClient = CreateClient();

    private static ApiClient CreateClient() => new("http://localhost:5000/");

    [RelayCommand]
    private async Task LoginAsync()
    {
        LoginError = string.Empty;
        try
        {
            var login = await _apiClient.LoginAsync(Username.Trim(), Password);
            TenantName = login.TenantName;
            UserInfo = $"{login.Username} · {login.Role}";
            IsLoggedIn = true;
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            LoginError = ex.Message;
        }
    }

    [RelayCommand]
    private void SignOut()
    {
        _apiClient = CreateClient();
        IsLoggedIn = false;
        Tasks.Clear();
        StatusText = string.Empty;
        SelectedTask = null;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadTasksAsync();

    [RelayCommand]
    private async Task CompleteAsync()
    {
        if (SelectedTask is null)
        {
            StatusText = "Select a task first.";
            return;
        }

        if (SelectedTask.IsCompleted)
        {
            StatusText = "Task is already completed.";
            return;
        }

        try
        {
            await _apiClient.CompleteTaskAsync(SelectedTask.Id);
            StatusText = "Task completed.";
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            var tasks = await _apiClient.GetTasksAsync();
            Tasks.Clear();
            foreach (var task in tasks)
                Tasks.Add(task);
            StatusText = $"{tasks.Count} task(s)";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}
