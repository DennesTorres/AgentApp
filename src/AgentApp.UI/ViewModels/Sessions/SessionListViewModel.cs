using System.Collections.ObjectModel;
using AgentApp.Application.Sessions;
using AgentApp.Domain.Sessions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Sessions;

public partial class SessionListViewModel : ObservableObject
{
    private readonly SessionService _sessionService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateSessionCommand))]
    private string _filterProjectId = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Rename support
    [ObservableProperty]
    private SessionItemViewModel? _selectedSession;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RenameSessionCommand))]
    private string _renameText = string.Empty;

    public ObservableCollection<SessionItemViewModel> Sessions { get; } = [];

    // C-042: suppress ClearSession during collection refresh
    public bool IsRefreshing { get; private set; }

    public SessionListViewModel(SessionService sessionService)
    {
        _sessionService = sessionService;
        // C-023: refresh list when any session is created (e.g. from Chat tab)
        _sessionService.SessionCreated += (_, _) => _ = RefreshAsync();
        // C-041: update item name directly when session renamed (no full refresh needed)
        _sessionService.SessionRenamed += OnSessionRenamed;
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
        var sessions = await _sessionService.GetAllActiveAsync();
        ApplyToCollection(sessions);

        // C-040: auto-select most recent session; create one if none exist
        if (Sessions.Count > 0)
            SelectedSession = Sessions[0];
        else
        {
            var session = await _sessionService.StartStandaloneSessionAsync();
            // SessionCreated event will call RefreshAsync which repopulates; select it
            SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        }
    }

    private void ApplyToCollection(IReadOnlyList<ChatSession> sessions)
    {
        Sessions.Clear();
        var filtered = string.IsNullOrWhiteSpace(FilterProjectId)
            ? sessions
            : sessions.Where(s => s.ProjectId.HasValue &&
                s.ProjectId.Value.ToString().Equals(FilterProjectId, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var s in filtered.OrderByDescending(x => x.CreatedAt))
            Sessions.Add(new SessionItemViewModel(s));
    }

    // US-184: Create a new standalone session — C-034: auto-select so chat loads with greeting
    [RelayCommand]
    private async Task CreateSessionAsync()
    {
        var session = await _sessionService.StartStandaloneSessionAsync();
        await RefreshAsync();
        SelectedSession = Sessions.FirstOrDefault(s => s.Id == session.Id);
        StatusMessage = "New session created.";
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
        await _sessionService.ArchiveAsync(item.Id);
        await RefreshAsync();
        StatusMessage = "Session archived.";
    }

    // US-188 / US-019: Filter by project — re-load and filter
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        var all = await _sessionService.GetAllActiveAsync();
        ApplyToCollection(all);
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        FilterProjectId = string.Empty;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        // C-042: preserve selection across refresh — Sessions.Clear() nullifies SelectedSession via ListBox binding
        var selectedId = SelectedSession?.Id;
        IsRefreshing = true;
        var sessions = await _sessionService.GetAllActiveAsync();
        ApplyToCollection(sessions);
        if (selectedId.HasValue)
            SelectedSession = Sessions.FirstOrDefault(s => s.Id == selectedId);
        IsRefreshing = false;
    }
}

public partial class SessionItemViewModel : ObservableObject
{
    public Guid Id { get; }
    public string ProjectLabel { get; }
    public string CreatedAt { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _renameBuffer = string.Empty;

    public SessionItemViewModel(ChatSession session)
    {
        Id = session.Id;
        _name = session.Name;
        ProjectLabel = session.ProjectId.HasValue ? session.ProjectId.Value.ToString()[..8] : "(standalone)";
        CreatedAt = session.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    public void BeginRename()
    {
        RenameBuffer = Name;
        IsRenaming = true;
    }

    public void CancelRename() => IsRenaming = false;
}
