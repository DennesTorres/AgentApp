using System.IO;
using System.Windows;
using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Projects;
using AgentApp.Application.Providers;
using AgentApp.Application.Scheduling;
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
using AgentApp.UI.Services;
using AgentApp.UI.ViewModels;
using AgentApp.UI.ViewModels.Board;
using AgentApp.UI.ViewModels.Chat;
using AgentApp.UI.ViewModels.Projects;
using AgentApp.UI.ViewModels.Sessions;
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

        // Infrastructure — project storage under %USERPROFILE%\.tower; other data under LocalApplicationData
        var towerRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tower");
        Directory.CreateDirectory(towerRoot);

        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgentApp");
        Directory.CreateDirectory(appDataFolder);

        services.AddSingleton<IProjectRepository>(_ => new JsonProjectRepository(towerRoot));
        services.AddSingleton<ISettingsRepository>(_ => new JsonSettingsRepository(appDataFolder));
        services.AddSingleton<IProjectSettingsRepository>(_ => new JsonProjectSettingsRepository(appDataFolder));
        services.AddSingleton<ISessionRepository>(_ => new JsonSessionRepository(appDataFolder));
        services.AddSingleton<ISessionMessageRepository>(_ => new JsonSessionMessageRepository(appDataFolder));
        services.AddSingleton<IKnowledgeRecordRepository>(_ => new JsonKnowledgeRecordRepository(appDataFolder));
        services.AddSingleton<IScheduleRepository>(_ => new JsonScheduleRepository(appDataFolder));
        services.AddSingleton<IProjectSwitchHandler, NullProjectSwitchHandler>();
        services.AddSingleton<ICredentialService, WindowsCredentialManager>();
        services.AddSingleton<IScaffoldService, ScaffoldService>();
        services.AddSingleton<IFilePermissionGate, FileSessionGate>();

        // Provider pipeline — file providers registered alongside model provider
        // C-037: load model URL + name from settings at startup
        services.AddSingleton<IProviderRegistry>(sp =>
        {
            var registry = new ProviderRegistry();
            var credentialService = sp.GetRequiredService<ICredentialService>();
            var settingsRepo = sp.GetRequiredService<ISettingsRepository>();
            var settings = Task.Run(() => settingsRepo.GetGlobalSettingsAsync()).GetAwaiter().GetResult();
            var chatClient = AzureClientFactory.BuildFromCredentials(
                credentialService,
                settings.ModelUrl,
                settings.ModelName);
            if (chatClient is not null)
                registry.Register(new AzureChatClientProvider(chatClient));
            registry.Register(new FileReadProvider());
            registry.Register(new FileWriteProvider());
            registry.Register(new DirectoryListProvider());
            return registry;
        });
        services.AddSingleton<CapabilityDispatcher>();
        services.AddSingleton<IChatCommandParser, ChatCommandParser>();
        services.AddSingleton<IResponsePreparationService, ResponsePreparationService>();
        services.AddSingleton<IActionProviderRegistry>(sp =>
        {
            var registry = new ActionProviderRegistry();
            registry.Register(new StartProjectActionProvider(
                sp.GetRequiredService<ProjectService>(),
                sp.GetRequiredService<IScaffoldService>(),
                sp.GetRequiredService<ISettingsRepository>(),
                sp.GetRequiredService<IAgentContextService>(),
                sp.GetRequiredService<IFilePermissionGate>()));
            return registry;
        });

        // Agent context + system message providers
        services.AddSingleton<IAgentContextService, AgentContextService>();
        services.AddSingleton<ISystemMessageProvider, InitializationPromptProvider>();
        services.AddSingleton<ISystemMessageProvider, ActiveProjectProvider>();
        services.AddSingleton<ISystemMessageProvider, ExecutionStateProvider>();
        services.AddSingleton<ISystemMessageProvider, FileToolsPromptProvider>();

        // Application
        services.AddSingleton<SessionService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<ProjectSwitchService>();
        services.AddSingleton<BoardService>();
        services.AddSingleton<SchedulerService>();
        services.AddSingleton<IOnboardingService>(sp => new OnboardingService(
            sp.GetRequiredService<IProjectRepository>(),
            sp.GetRequiredService<ISettingsRepository>()));
        services.AddSingleton<ChatOrchestrator>(sp => new ChatOrchestrator(
            sp.GetRequiredService<CapabilityDispatcher>(),
            sp.GetRequiredService<IResponsePreparationService>(),
            sp.GetRequiredService<IActionProviderRegistry>(),
            sp.GetRequiredService<IFilePermissionGate>(),
            sp.GetRequiredService<IScaffoldService>(),
            sp.GetRequiredService<ProjectService>(),
            sp.GetRequiredService<ISettingsRepository>(),
            sp.GetRequiredService<IOnboardingService>(),
            sp.GetRequiredService<IAgentContextService>(),
            sp.GetServices<ISystemMessageProvider>().ToArray()));

        // UI
        services.AddSingleton<ChatPresenter>();
        services.AddSingleton<ChatViewModel>(sp => new ChatViewModel(
            sp.GetRequiredService<ChatPresenter>(),
            sp.GetRequiredService<SessionService>(),
            sp.GetRequiredService<ISettingsRepository>()));
        services.AddSingleton<ProjectListViewModel>();
        services.AddSingleton<SessionListViewModel>();
        services.AddSingleton<BoardViewModel>();
        services.AddSingleton<GlobalSettingsViewModel>();
        services.AddSingleton<ProjectSettingsViewModel>();
        services.AddSingleton<ApiKeySettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
