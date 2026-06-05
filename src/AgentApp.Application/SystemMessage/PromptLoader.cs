using System.IO;
using System.Reflection;

namespace AgentApp.Application.SystemMessage;

public static class PromptLoader
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public static string Load(string resourceName, string? overridePath = null)
    {
        if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
            return File.ReadAllText(overridePath);

        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return string.Empty;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
