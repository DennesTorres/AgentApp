using System.IO;
using System.Windows;
using AgentApp.Application.Agent;
using AgentApp.Application.Chat;
using AgentApp.Application.Context;
using AgentApp.Application.FileSystem;
using AgentApp.Application.Filters;
using AgentApp.Application.Gates;
using AgentApp.Application.Onboarding;
using AgentApp.Application.Orchestration;
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

        // C-080: start background scheduler check (Epic 12)
        var schedulerTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        schedulerTimer.Tick += async (_, _) =>
        {
            var scheduler = Services.GetRequiredService<SchedulerService>();
            if (await scheduler.IsDueAsync(Domain.Scheduling.ScheduledJobType.ReviewAgent))
                await scheduler.RecordRunAsync(Domain.Scheduling.ScheduledJobType.ReviewAgent);
        };
        schedulerTimer.Start();
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
        services.AddSingleton<IConversationHistoryRepository>(_ => new JsonConversationHistoryRepository(appDataFolder));
        services.AddSingleton<IMdFileRepository>(_ => new JsonMdFileRepository(towerRoot));
        services.AddSingleton<ITriggersIndexRepository>(_ => new JsonTriggersIndexRepository(towerRoot));
        services.AddSingleton<IFilterRuleRepository>(_ => new JsonFilterRuleRepository(appDataFolder));
        services.AddSingleton<IGateRuleRepository>(_ => new JsonGateRuleRepository(appDataFolder));
        services.AddSingleton<IRollingWindowStore>(_ => new FileSystemRollingWindowStore(appDataFolder));
        services.AddSingleton<IRollingWindowRuleRepository>(_ => new JsonRollingWindowRuleRepository(appDataFolder));
        services.AddSingleton<IOrchestratorSessionRepository>(_ => new JsonOrchestratorSessionRepository(appDataFolder));
        services.AddSingleton<IReasoningTraceRepository>(_ => new JsonReasoningTraceRepository(appDataFolder));

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
            registry.Register(new NameConfirmActionProvider(
                sp.GetRequiredService<IAgentContextService>(),
                sp.GetRequiredService<IProjectRepository>()));
            return registry;
        });

        // Agent context + system message providers
        services.AddSingleton<IAgentContextService, AgentContextService>();
        services.AddSingleton<ISystemMessageProvider, AgentContextProvider>();       // always — canonical state (stage field)
        services.AddSingleton<ISystemMessageProvider, AgentFoundationProvider>();   // always — persona + command format
        services.AddSingleton<ISystemMessageProvider, StagePromptProvider>();       // always — stage 1 reactive signal guidance
        services.AddSingleton<ISystemMessageProvider, NameConfirmationProvider>();  // HasProject && !NameConfirmed
        services.AddSingleton<ISystemMessageProvider, FileToolsPromptProvider>();   // HasProject — file access instructions
        // Parked: ActiveProjectProvider and ExecutionStateProvider — state now in AgentContextProvider;
        // re-register when behavioral instructions are developed for these concerns.

        // Orchestration + pipeline services
        services.AddSingleton<ContextAssembler>();
        services.AddSingleton<FilterPipeline>();
        services.AddSingleton<GateValidator>();
        services.AddSingleton<ContextWindowManager>();
        services.AddSingleton<RollingWindowManager>();
        services.AddSingleton<ReviewOrchestratorService>();

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
            sp.GetServices<ISystemMessageProvider>().ToArray(),
            sp.GetRequiredService<IProjectSettingsRepository>(),
            sp.GetRequiredService<ISessionRepository>(),
            sp.GetRequiredService<SessionService>(),
            sp.GetRequiredService<ContextAssembler>(),
            sp.GetRequiredService<IReasoningTraceRepository>(),
            sp.GetRequiredService<FilterPipeline>(),
            sp.GetRequiredService<GateValidator>(),
            sp.GetRequiredService<IGateRuleRepository>(),
            sp.GetRequiredService<ContextWindowManager>(),
            sp.GetRequiredService<RollingWindowManager>(),
            sp.GetRequiredService<BoardService>()));

        // UI
        services.AddSingleton<ChatPresenter>();
        services.AddSingleton<ChatViewModel>(sp => new ChatViewModel(
            sp.GetRequiredService<ChatPresenter>(),
            sp.GetRequiredService<SessionService>(),
            sp.GetRequiredService<ISettingsRepository>()));
        services.AddSingleton<ProjectListViewModel>();
        services.AddSingleton<SessionListViewModel>(sp => new SessionListViewModel(
            sp.GetRequiredService<SessionService>(),
            sp.GetRequiredService<ProjectService>()));
        services.AddSingleton<BoardViewModel>();
        services.AddSingleton<GlobalSettingsViewModel>();
        services.AddSingleton<ProjectSettingsViewModel>();
        services.AddSingleton<ApiKeySettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
