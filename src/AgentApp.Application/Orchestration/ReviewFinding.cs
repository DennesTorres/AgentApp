namespace AgentApp.Application.Orchestration;

public class ReviewFinding
{
    public string Title { get; }
    public string Description { get; }

    public ReviewFinding(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
