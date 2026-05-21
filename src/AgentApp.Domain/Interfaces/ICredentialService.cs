namespace AgentApp.Domain.Interfaces;

public interface ICredentialService
{
    string? ReadCredential(string credentialName);
    void WriteCredential(string credentialName, string secret);
}
