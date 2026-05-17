using System.IO;
using System.Windows;
using AgentApp.Application.Chat;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Application.Sessions;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.ProjectSwitch;
using AgentApp.UI.ViewModels;
using AgentApp.UI.ViewModels.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace AgentApp.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Services = ConfigureServices();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure — storage folder under LocalApplicationData
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgentApp");
        Directory.CreateDirectory(appDataFolder);

        services.AddSingleton<IProjectRepository>(_ => new JsonProjectRepository(appDataFolder));
        services.AddSingleton<ISettingsRepository>(_ => new JsonSettingsRepository(appDataFolder));
        services.AddSingleton<IProjectSwitchHandler, NullProjectSwitchHandler>();
        services.AddSingleton<ICredentialService, WindowsCredentialManager>();

        // Provider pipeline — model provider registered if API key is present in credential store
        services.AddSingleton<IProviderRegistry>(sp =>
        {
            var registry = new ProviderRegistry();
            var credentialService = sp.GetRequiredService<ICredentialService>();
            var chatClient = AzureClientFactory.BuildFromCredentials(credentialService);
            if (chatClient is not null)
                registry.Register(new AzureChatClientProvider(chatClient));
            return registry;
        });
        services.AddSingleton<OrchestratorPipeline>();

        // Application
        services.AddSingleton<SessionService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<ProjectSwitchService>();
        services.AddSingleton<ChatService>();

        // UI
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
