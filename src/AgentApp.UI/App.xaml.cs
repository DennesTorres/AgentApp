using System.IO;
using System.Windows;
using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Application.Sessions;
using AgentApp.Application.SystemMessage;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.FileSystem;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.ProjectSwitch;
using AgentApp.Infrastructure.Scaffold;
using AgentApp.UI.ViewModels;
using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Projects;
using AgentApp.UI.ViewModels.Settings;
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
        services.AddSingleton<IScaffoldService, ScaffoldService>();
        services.AddSingleton<IFilePermissionGate, FileSessionGate>();

        // Provider pipeline — file providers registered alongside model provider
        services.AddSingleton<IProviderRegistry>(sp =>
        {
            var registry = new ProviderRegistry();
            var credentialService = sp.GetRequiredService<ICredentialService>();
            var chatClient = AzureClientFactory.BuildFromCredentials(credentialService);
            if (chatClient is not null)
                registry.Register(new AzureChatClientProvider(chatClient));
            registry.Register(new FileReadProvider());
            registry.Register(new FileWriteProvider());
            registry.Register(new DirectoryListProvider());
            return registry;
        });
        services.AddSingleton<OrchestratorPipeline>();
        services.AddSingleton<IChatCommandParser, ChatCommandParser>();

        // Agent context + system message providers
        services.AddSingleton<IAgentContextService, AgentContextService>();
        services.AddSingleton<ISystemMessageProvider, NoProjectProvider>();
        services.AddSingleton<ISystemMessageProvider, ActiveProjectProvider>();
        services.AddSingleton<ISystemMessageProvider, ExecutionStateProvider>();

        // Application
        services.AddSingleton<SessionService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<ProjectSwitchService>();
        services.AddSingleton<IOnboardingService>(sp => new OnboardingService(
            sp.GetRequiredService<IProjectRepository>(),
            sp.GetRequiredService<ISettingsRepository>()));
        services.AddSingleton<ChatService>(sp => new ChatService(
            sp.GetRequiredService<OrchestratorPipeline>(),
            sp.GetRequiredService<IChatCommandParser>(),
            sp.GetServices<ISystemMessageProvider>().ToArray(),
            sp.GetRequiredService<IAgentContextService>()));

        // UI
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<ProjectListViewModel>();
        services.AddSingleton<ApiKeySettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
