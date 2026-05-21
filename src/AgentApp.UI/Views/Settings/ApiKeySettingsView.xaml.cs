using System.Windows.Controls;
using AgentApp.UI.ViewModels.Settings;

namespace AgentApp.UI.Views.Settings;

public partial class ApiKeySettingsView : UserControl
{
    public ApiKeySettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SyncPasswordBox();
    }

    private void ApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ApiKeySettingsViewModel vm)
            vm.ApiKey = ApiKeyBox.Password;
    }

    private void SyncPasswordBox()
    {
        if (DataContext is ApiKeySettingsViewModel vm)
            ApiKeyBox.Password = vm.ApiKey;
    }
}
