using AgentApp.Domain.Learning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentApp.UI.ViewModels;

public partial class LearningProposalViewModel : ObservableObject
{
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _humanSummary = string.Empty;
    [ObservableProperty] private string _ruleText = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;

    private LearningSession? _activeSession;

    public void ShowProposal(LearningSession session)
    {
        if (session.ProposedChange == null) return;
        _activeSession = session;
        HumanSummary = session.ProposedChange.HumanSummary;
        RuleText = session.ProposedChange.RuleText;
        FileName = session.ProposedChange.FileName;
        IsVisible = true;
    }

    [RelayCommand]
    private void Approve()
    {
        _activeSession?.Approve();
        IsVisible = false;
    }

    [RelayCommand]
    private void Reject()
    {
        _activeSession?.Reject();
        IsVisible = false;
    }
}
