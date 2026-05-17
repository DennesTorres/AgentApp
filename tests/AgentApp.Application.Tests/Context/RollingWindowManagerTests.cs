using AgentApp.Application.Context;
using AgentApp.Domain.Interfaces;
using AgentApp.Domain.Rules;
using NSubstitute;

namespace AgentApp.Application.Tests.Context;

public class RollingWindowManagerTests
{
    private readonly IRollingWindowStore _store;
    private readonly IRollingWindowRuleRepository _ruleRepository;
    private readonly RollingWindowManager _sut;

    public RollingWindowManagerTests()
    {
        _store = Substitute.For<IRollingWindowStore>();
        _ruleRepository = Substitute.For<IRollingWindowRuleRepository>();
        _sut = new RollingWindowManager(_store, _ruleRepository);

        _store.ReadActiveAsync(Arg.Any<string>()).Returns(string.Empty);
    }

    [Fact]
    public async Task AppendAsync_EmptyFile_WritesContentDirectly()
    {
        _store.ReadActiveAsync("session-memory").Returns(string.Empty);

        await _sut.AppendAsync("session-memory", "New session content.", MdFileScope.Global, null);

        await _store.Received(1).WriteActiveAsync("session-memory", "New session content.");
    }

    [Fact]
    public async Task AppendAsync_ExistingContent_AppendsWithSeparator()
    {
        _store.ReadActiveAsync("session-memory").Returns("Existing content.");

        await _sut.AppendAsync("session-memory", "Appended content.", MdFileScope.Global, null);

        await _store.Received(1).WriteActiveAsync("session-memory",
            "Existing content.\n\nAppended content.");
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_ContentBelowLimit_DoesNotArchive()
    {
        var rule = RollingWindowRule.Create("session-memory", 1000, 30, MdFileScope.Global, null);
        _ruleRepository.GetByFileTypeAsync("session-memory", MdFileScope.Global, null).Returns(rule);
        _store.ReadActiveAsync("session-memory").Returns("Short content.");

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        await _store.DidNotReceive().ArchiveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_ContentOverLimit_ArchivesAndClears()
    {
        var rule = RollingWindowRule.Create("session-memory", 10, 30, MdFileScope.Global, null);
        _ruleRepository.GetByFileTypeAsync("session-memory", MdFileScope.Global, null).Returns(rule);
        _store.ReadActiveAsync("session-memory").Returns("This content is over the limit.");

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        await _store.Received(1).ArchiveAsync("session-memory", "This content is over the limit.");
        await _store.Received(1).WriteActiveAsync("session-memory", string.Empty);
    }

    [Fact]
    public async Task ArchiveIfNeededAsync_NoRule_DoesNothing()
    {
        _ruleRepository.GetByFileTypeAsync(Arg.Any<string>(), Arg.Any<MdFileScope>(), Arg.Any<Guid?>())
            .Returns((RollingWindowRule?)null);

        await _sut.ArchiveIfNeededAsync("session-memory", MdFileScope.Global, null);

        await _store.DidNotReceive().ArchiveAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SearchArchivesAsync_DelegatesToStore()
    {
        _store.SearchArchivesAsync("query", "session-memory")
            .Returns(new List<string> { "match 1", "match 2" });

        var results = await _sut.SearchArchivesAsync("query", "session-memory", null);

        Assert.Equal(2, results.Count);
        await _store.Received(1).SearchArchivesAsync("query", "session-memory");
    }
}
