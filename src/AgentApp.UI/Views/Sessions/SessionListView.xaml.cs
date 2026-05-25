using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgentApp.UI.ViewModels.Sessions;

namespace AgentApp.UI.Views.Sessions;

public partial class SessionListView : UserControl
{
    public SessionListView()
    {
        InitializeComponent();
    }

    // C-032/C-052: double-click on session name enters inline rename mode
    private void NamePanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is SessionItemViewModel item)
            ((SessionListViewModel)DataContext!).BeginRenameItemCommand.Execute(item);
    }

    // C-052: auto-focus and select-all when rename TextBox becomes visible
    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && sender is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
    }

    // C-032: Enter commits rename; Escape cancels
    private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.DataContext is not SessionItemViewModel item) return;
        var vm = (SessionListViewModel)DataContext!;
        if (e.Key == Key.Enter)
        {
            vm.CommitRenameCommand.Execute(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CancelRenameItemCommand.Execute(item);
            e.Handled = true;
        }
    }

    // C-032: losing focus commits rename (only if still in rename mode)
    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is SessionItemViewModel item && item.IsRenaming)
            ((SessionListViewModel)DataContext!).CommitRenameCommand.Execute(item);
    }
}
