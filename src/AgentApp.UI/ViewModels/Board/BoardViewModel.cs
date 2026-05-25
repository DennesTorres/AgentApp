using System.Collections.ObjectModel;
using AgentApp.Application.Projects;
using AgentApp.Application.Scheduling;
using AgentApp.Domain.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels.Board;

public partial class BoardViewModel : ObservableObject
{
    private readonly BoardService _boardService;
    private readonly SchedulerService _schedulerService;

    [ObservableProperty]
    private string _currentProjectId = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isReviewRunning;

    public ObservableCollection<KnowledgeRecordItemViewModel> Records { get; } = [];
    public ObservableCollection<SchedulerStatusItemViewModel> SchedulerStatuses { get; } = [];

    public BoardViewModel(BoardService boardService, SchedulerService schedulerService)
    {
        _boardService = boardService;
        _schedulerService = schedulerService;
        _ = LoadSchedulerStatusAsync();
    }

    // US-091/US-107: Load board records for a project
    [RelayCommand]
    private async Task LoadBoardAsync()
    {
        if (!Guid.TryParse(CurrentProjectId, out var projectId))
        {
            StatusMessage = "Enter a valid project ID to load the board.";
            return;
        }

        var records = await _boardService.GetBoardAsync(projectId);
        Records.Clear();
        foreach (var r in records.OrderBy(x => x.Status))
            Records.Add(new KnowledgeRecordItemViewModel(r));

        StatusMessage = $"Loaded {records.Count} records.";
    }

    // US-112: Trigger Review Agent
    [RelayCommand]
    private async Task TriggerReviewAgentAsync()
    {
        IsReviewRunning = true;
        StatusMessage = "Review agent triggered — running…";
        // Scheduler records the run; actual dual-agent pipeline (Epic 11) picks it up
        await _schedulerService.RecordRunAsync(Domain.Scheduling.ScheduledJobType.ReviewAgent);
        StatusMessage = "Review agent run recorded. Agent will process on next cycle.";
        IsReviewRunning = false;
        await LoadSchedulerStatusAsync();
    }

    // US-113: Load scheduler status
    [RelayCommand]
    private async Task LoadSchedulerStatusAsync()
    {
        var statuses = await _schedulerService.GetAllStatusesAsync();
        SchedulerStatuses.Clear();
        foreach (var s in statuses)
            SchedulerStatuses.Add(new SchedulerStatusItemViewModel(s));
    }
}

public class KnowledgeRecordItemViewModel
{
    public string Title { get; }
    public string Status { get; }
    public string RecordType { get; }

    public KnowledgeRecordItemViewModel(KnowledgeRecord record)
    {
        Title = record.Title;
        Status = record.Status.ToString();
        RecordType = record.RecordType.ToString();
    }
}

public class SchedulerStatusItemViewModel
{
    public string JobType { get; }
    public string IsEnabled { get; }
    public string LastRun { get; }
    public string NextRun { get; }

    public SchedulerStatusItemViewModel(SchedulerJobStatus status)
    {
        JobType = status.JobType.ToString();
        IsEnabled = status.IsEnabled ? "Enabled" : "Disabled";
        LastRun = status.LastRunAt.HasValue
            ? status.LastRunAt.Value.LocalDateTime.ToString("yyyy-MM-dd HH:mm")
            : "Never";
        NextRun = status.NextRunAt.HasValue
            ? status.NextRunAt.Value.LocalDateTime.ToString("yyyy-MM-dd HH:mm")
            : "—";
    }
}
