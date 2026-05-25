using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgentApp.UI.ViewModels;
using AgentApp.UI.ViewModels.Sessions;
using MahApps.Metro.Controls;

namespace AgentApp.UI;

public partial class MainWindow : MetroWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SessionItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: SessionItemViewModel item })
            item.BeginRename();
    }

    private void RenameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: SessionItemViewModel item }) return;
        if (e.Key == Key.Enter)
        {
            var vm = (MainWindowViewModel)DataContext;
            vm.SessionListViewModel.CommitRenameCommand.Execute(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            item.CancelRename();
            e.Handled = true;
        }
    }

    private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: SessionItemViewModel item }) return;
        if (!item.IsRenaming) return;
        var vm = (MainWindowViewModel)DataContext;
        if (!string.IsNullOrWhiteSpace(item.RenameBuffer))
            vm.SessionListViewModel.CommitRenameCommand.Execute(item);
        else
            item.CancelRename();
    }
}