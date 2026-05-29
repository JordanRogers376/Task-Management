using System.Windows;
using TaskManagement.Desktop.Services;

namespace TaskManagement.Desktop;

public partial class MainWindow : Window
{
    private ApiClient _apiClient = new("http://localhost:5000/");

    public MainWindow()
    {
        InitializeComponent();
        PasswordBox.Password = "Password123!";
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginErrorText.Text = string.Empty;
        try
        {
            var login = await _apiClient.LoginAsync(EmailBox.Text.Trim(), PasswordBox.Password);
            TenantNameText.Text = login.TenantName;
            UserInfoText.Text = $"{login.Email} · {login.Role}";
            LoginPanel.Visibility = Visibility.Collapsed;
            TasksPanel.Visibility = Visibility.Visible;
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            LoginErrorText.Text = ex.Message;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadTasksAsync();

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        _apiClient = new ApiClient("http://localhost:5000/");
        TasksPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        TasksGrid.ItemsSource = null;
        StatusText.Text = string.Empty;
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (TasksGrid.SelectedItem is not ApiClient.TaskDto selected)
        {
            StatusText.Text = "Select a task first.";
            return;
        }

        if (selected.IsCompleted)
        {
            StatusText.Text = "Task is already completed.";
            return;
        }

        try
        {
            await _apiClient.CompleteTaskAsync(selected.Id);
            StatusText.Text = "Task completed.";
            await LoadTasksAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private async Task LoadTasksAsync()
    {
        try
        {
            var tasks = await _apiClient.GetTasksAsync();
            TasksGrid.ItemsSource = tasks;
            StatusText.Text = $"{tasks.Count} task(s)";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }
}
