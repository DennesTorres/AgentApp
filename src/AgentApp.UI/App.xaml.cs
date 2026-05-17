using System.IO;
using System.Windows;
using AgentApp.Application.Chat;
using AgentApp.Application.ExecutionStates;
using AgentApp.Application.Learning;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Application.Sessions;
using AgentApp.Domain.ExecutionStates;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Providers;
using AgentApp.Infrastructure.Credentials;
using AgentApp.Infrastructure.ModelAccess;
using AgentApp.Infrastructure.Persistence;
using AgentApp.Infrastructure.ProjectSwitch;
using AgentApp.UI.ViewModels;
using AgentApp.UI.ViewModels.Chat;
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

        // Execution state machine — register four state providers, then registry, then machine
        services.AddSingleton<IExecutionStateRegistry>(sp =>
        {
            var registry = new ExecutionStateRegistry();
            registry.Register(new ChatStateProvider());
            registry.Register(new ResearchStateProvider());
            registry.Register(new ImplementingStateProvider());
            registry.Register(new TestingStateProvider());
            return registry;
        });
        services.AddSingleton<IExecutionStateMachine, ExecutionStateMachine>();
        services.AddSingleton<StateAwareSystemMessageBuilder>();

        // Learning orchestrator
        services.AddSingleton<LearningStack>();
        services.AddSingleton<LearningOrchestrator>();

        // Application
        services.AddSingleton<SessionService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<ProjectSwitchService>();
        services.AddSingleton<ChatService>();

        // UI
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<ApiKeySettingsViewModel>();
        services.AddSingleton<LearningProposalViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
