namespace AgentApp.Domain.Interfaces;

public interface ILearningLogger
{
    Task LogAsync(string entry);
}
