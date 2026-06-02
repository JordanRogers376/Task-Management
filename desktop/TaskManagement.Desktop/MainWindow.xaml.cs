using System.Windows;
using TaskManagement.Desktop.ViewModels;

namespace TaskManagement.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;
        PasswordBox.Password = viewModel.Password;
        PasswordBox.PasswordChanged += (_, _) => viewModel.Password = PasswordBox.Password;
    }
}
