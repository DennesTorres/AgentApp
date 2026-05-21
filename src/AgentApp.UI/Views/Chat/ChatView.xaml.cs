using System.Windows.Controls;
using System.Windows.Input;
using AgentApp.UI.ViewModels.Chat;

namespace AgentApp.UI.Views.Chat;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return)
            return;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            return; // Shift+Enter: let AcceptsReturn insert newline

        e.Handled = true;

        if (DataContext is ChatViewModel vm && vm.SendMessageCommand.CanExecute(null))
            vm.SendMessageCommand.Execute(null);
    }
}
