using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using AgentApp.Application.Projects;
using AgentApp.Application.Sessions;
using AgentApp.Domain.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Sessions;

public record ProjectGroupKey(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

public partial class SessionListViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly ProjectService _projectService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private SessionItemViewModel? _selectedSession;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameSessionCommand))]
    private string _renameText = string.Empty;

    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];

    // US-184/US-185: grouped view — project header per group, sessions inside
    public ICollectionView SessionsView { get; }

    // C-069: available projects for the new-session project selector; null entry = standalone session
    public ObservableCollection<ProjectGroupKey> AvailableProjects { get; } = [];

    [ObservableProperty]
    private ProjectGroupKey? _selectedNewSessionProject;

    // C-042: suppress ClearSession during collection refresh
    public bool IsRefreshing { get; private set; }

    // C-057: raised when the user explicitly creates a new session — navigate to Chat tab
    public event Action? NavigateToChatRequested;

    public SessionListViewModel(SessionService sessionService, ProjectService projectService)
    {
        _sessionService = sessionService;
        _projectService = projectService;

        SessionsView = CollectionViewSource.GetDefaultView(Sessions);
        SessionsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SessionItemViewModel.ProjectGroup)));
        SessionsView.SortDescriptions.Add(new SortDescription(nameof(SessionItemViewModel.GroupSortKey), ListSortDirection.Ascending));
        SessionsView.SortDescriptions.Add(new SortDescription(nameof(SessionItemViewModel.CreatedAtTicks), ListSortDirection.Descending));

        // C-023: refresh list when any session is created (e.g. from Chat tab)
        _sessionService.SessionCreated += (_, _) => _ = RefreshAsync();
        // C-041: update item name directly when session renamed (no full refresh needed)
        _sessionService.SessionRenamed += OnSessionRenamed;
        // C-085: refresh when a session is linked to a project (group header update)
        _sessionService.SessionLinkedToProject += (_, _) => _ = RefreshAsync();
        _ = LoadSessionsAsync();
    }

    // C-041: update the matching SessionItemViewModel name without full refresh
    private void OnSessionRenamed(object? sender, (Guid sessionId, string newName) e)
    {
        var item = Sessions.FirstOrDefault(s => s.Id == e.sessionId);
        if (item is not null)
            item.Name = e.newName;
    }

    private async Task LoadSessionsAsync()
    {
        var projectMap = await BuildProjectMapAsync();
        var sessions = await _sessionService.GetAllActiveAsync();
        ApplyToCollection(sessions, projectMap);

        // C-040: auto-select most recent session; create one if none exist
        if (Sessions.Count > 0)
            SelectedSession = Sessions.OrderByDescending(s => s.CreatedAtTicks).First();
        else
        {
            var session = await _sessionService.StartStandaloneSessionAsync();
            SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        }
    }

    private async Task<Dictionary<Guid, string>> BuildProjectMapAsync()
    {
        var projects = await _projectService.GetAllProjectsAsync();

        // C-069: refresh the project selector options
        AvailableProjects.Clear();
        AvailableProjects.Add(new ProjectGroupKey(null, "(No Project — standalone)"));
        foreach (var p in projects.OrderBy(p => p.Name))
            AvailableProjects.Add(new ProjectGroupKey(p.Id, p.Name));

        // Keep selection valid: if selected project no longer exists, reset to standalone
        if (SelectedNewSessionProject?.Id.HasValue == true &&
            !projects.Any(p => p.Id == SelectedNewSessionProject.Id))
            SelectedNewSessionProject = null;

        return projects.ToDictionary(p => p.Id, p => p.Name);
    }

    private void ApplyToCollection(IReadOnlyList<ChatSession> sessions, Dictionary<Guid, string> projectMap)
    {
        Sessions.Clear();
        foreach (var s in sessions)
            Sessions.Add(new SessionItemViewModel(s, projectMap));
    }

    // C-086: Open project — navigate to most recent session for this project, or create one if none exists
    public async Task OpenProjectSessionAsync(Guid projectId)
    {
        var projectSessions = await _sessionService.GetByProjectIdAsync(projectId);
        var activeSessions = projectSessions.Where(s => !s.IsArchived).OrderByDescending(s => s.CreatedAt).ToList();

        ChatSession session;
        if (activeSessions.Count > 0)
        {
            session = activeSessions[0];
            await RefreshAsync();
        }
        else
        {
            session = await _sessionService.CreateForProjectAsync(projectId);
            await RefreshAsync();
        }
        SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        NavigateToChatRequested?.Invoke();
    }

    // US-184/C-069: Create a new session — standalone or project-linked based on selector
    [RelayCommand]
    private async Task CreateSessionAsync()
    {
        ChatSession session;
        string statusMsg;
        if (SelectedNewSessionProject?.Id.HasValue == true)
        {
            session = await _sessionService.CreateForProjectAsync(SelectedNewSessionProject.Id.Value);
            statusMsg = $"New session created in project \"{SelectedNewSessionProject.Name}\".";
        }
        else
        {
            session = await _sessionService.StartStandaloneSessionAsync();
            statusMsg = "New session created.";
        }
        await RefreshAsync();
        SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        StatusMessage = statusMsg;
        NavigateToChatRequested?.Invoke();
    }

    // US-185: Create a new session inside a project group
    [RelayCommand]
    private async Task CreateSessionInGroupAsync(ProjectGroupKey? group)
    {
        ChatSession session;
        if (group?.Id.HasValue == true)
            session = await _sessionService.CreateForProjectAsync(group.Id.Value);
        else
            session = await _sessionService.StartStandaloneSessionAsync();
        await RefreshAsync();
        SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        StatusMessage = group?.Id.HasValue == true
            ? $"New session created in project \"{group.Name}\"."
            : "New session created.";
        NavigateToChatRequested?.Invoke();
    }

    // US-186: Rename selected session (full-view rename panel)
    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task RenameSessionAsync()
    {
        if (SelectedSession is null) return;
        await _sessionService.RenameAsync(SelectedSession.Id, RenameText.Trim());
        await RefreshAsync();
        RenameText = string.Empty;
        StatusMessage = "Session renamed.";
    }

    private bool CanRename() => !string.IsNullOrWhiteSpace(RenameText) && SelectedSession is not null;

    // C-032: Inline rename — commit the rename from sidebar TextBox
    [RelayCommand]
    private async Task CommitRenameAsync(SessionItemViewModel? item)
    {
        if (item is null || string.IsNullOrWhiteSpace(item.RenameBuffer)) return;
        await _sessionService.RenameAsync(item.Id, item.RenameBuffer.Trim());
        item.Name = item.RenameBuffer.Trim();
        item.IsRenaming = false;
    }

    [RelayCommand]
    private static void BeginRenameItem(SessionItemViewModel? item) => item?.BeginRename();

    [RelayCommand]
    private static void CancelRenameItem(SessionItemViewModel? item) => item?.CancelRename();

    // US-187: Archive selected session
    [RelayCommand]
    private async Task ArchiveSessionAsync(SessionItemViewModel? item)
    {
        if (item is null) return;
        // C-050: track whether the archived session was the selected one
        var wasSelected = SelectedSession?.Id == item.Id;
        await _sessionService.ArchiveAsync(item.Id);
        await RefreshAsync();
        // C-050: if the archived session was selected, explicitly pick a replacement
        if (wasSelected)
        {
            if (Sessions.Count > 0)
                SelectedSession = Sessions.OrderByDescending(s => s.CreatedAtTicks).First();
            else
            {
                var newSession = await _sessionService.StartStandaloneSessionAsync();
                await RefreshAsync();
                SelectedSession = Sessions.FirstOrDefault(s => s.Id == newSession.Id);
            }
        }
        StatusMessage = "Session archived.";
    }

    private async Task RefreshAsync()
    {
        // C-042: preserve selection across refresh
        var selectedId = SelectedSession?.Id;
        IsRefreshing = true;
        var projectMap = await BuildProjectMapAsync();
        var sessions = await _sessionService.GetAllActiveAsync();
        ApplyToCollection(sessions, projectMap);
        if (selectedId.HasValue)
            SelectedSession = Sessions.FirstOrDefault(s => s.Id == selectedId);
        IsRefreshing = false;
    }
}

public partial class SessionItemViewModel : ObservableObject
{
    public Guid Id { get; }
    public ProjectGroupKey ProjectGroup { get; }
    public string GroupSortKey { get; }
    public long CreatedAtTicks { get; }
    public string CreatedAt { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _renameBuffer = string.Empty;

    public SessionItemViewModel(ChatSession session, Dictionary<Guid, string> projectMap)
    {
        Id = session.Id;
        _name = session.Name;
        CreatedAtTicks = session.CreatedAt.Ticks;
        CreatedAt = session.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        var projectName = session.ProjectId.HasValue && projectMap.TryGetValue(session.ProjectId.Value, out var n)
            ? n : "(No Project)";
        ProjectGroup = new ProjectGroupKey(session.ProjectId, projectName);
        // Sort: project sessions first (by name), standalone last
        GroupSortKey = session.ProjectId.HasValue ? projectName : "~";
    }

    public void BeginRename()
    {
        RenameBuffer = Name;
        IsRenaming = true;
    }

    public void CancelRename() => IsRenaming = false;
}
