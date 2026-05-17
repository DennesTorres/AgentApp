using AgentApp.UI.ViewModels;
using MahApps.Metro.Controls;

namespace AgentApp.UI;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}