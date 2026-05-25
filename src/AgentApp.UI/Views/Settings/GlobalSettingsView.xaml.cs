using System.Windows.Controls;
using AgentApp.UI.ViewModels.Settings;

namespace AgentApp.UI.Views.Settings;

public partial class GlobalSettingsView : UserControl
{
    public GlobalSettingsView()
    {
        InitializeComponent();
    }

    // C-026: PasswordBox can't bind normally — push to ViewModel on change
    private void NewApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is GlobalSettingsViewModel vm)
            vm.NewApiKey = ((PasswordBox)sender).Password;
    }
}
