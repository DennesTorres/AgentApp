using AgentApp.Domain.Agent;
using AgentApp.Domain.Interfaces;

namespace AgentApp.Application.SystemMessage;

public class StagePromptProvider : ISystemMessageProvider
{
    // C-066-R16: always active — emits stage-specific behavioral guidance.
    // Stage 1: instructs model how to handle structured JSON signals from file tools.
    // Stages 2-3: empty (project is set; FileToolsPromptProvider handles file access).
    public bool IsApplicable(AgentContext context) => true;

    public string GetSection(AgentContext context)
    {
        if (context.HasProject) return string.Empty;

        return $"Check your {AgentContextProvider.SectionRef}. Stage 1 is active — no project is set. " +
               "If a file tool returns a JSON object with \"signal\": \"stage_required\": " +
               "if the user has already expressed what they want to build, emit [STARTPROJECT:...] — " +
               "otherwise ask one short natural question. " +
               "Either way, do not call any file tool in the same response as [STARTPROJECT:...]; " +
               "they are mutually exclusive. Do not mention stage numbers, signals, or internal system names.";
    }
}
