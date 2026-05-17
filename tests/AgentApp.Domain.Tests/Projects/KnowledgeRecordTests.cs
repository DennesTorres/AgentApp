using AgentApp.Domain.Exceptions;
using AgentApp.Domain.Projects;

namespace AgentApp.Domain.Tests.Projects;

public class KnowledgeRecordTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var projectId = Guid.NewGuid();

        var record = KnowledgeRecord.Create(projectId, "US-001 Start standalone session",
            "User can start a standalone chat session.", KnowledgeRecordType.Story, null);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(projectId, record.ProjectId);
        Assert.Equal("US-001 Start standalone session", record.Title);
        Assert.Equal(KnowledgeRecordStatus.Backlog, record.Status);
        Assert.Equal(KnowledgeRecordType.Story, record.RecordType);
        Assert.Null(record.ParentId);
        Assert.Equal(1, record.FormatVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ThrowsDomainValidationException(string title)
    {
        Assert.Throws<DomainValidationException>(() =>
            KnowledgeRecord.Create(Guid.NewGuid(), title, "Description", KnowledgeRecordType.Story, null));
    }

    [Fact]
    public void TransitionTo_BacklogToInImplementation_Succeeds()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);

        record.TransitionTo(KnowledgeRecordStatus.InImplementation);

        Assert.Equal(KnowledgeRecordStatus.InImplementation, record.Status);
    }

    [Fact]
    public void TransitionTo_InImplementationToImplemented_Succeeds()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);

        record.TransitionTo(KnowledgeRecordStatus.Implemented);

        Assert.Equal(KnowledgeRecordStatus.Implemented, record.Status);
    }

    [Fact]
    public void TransitionTo_ImplementedToReviewedUser_Succeeds()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        record.TransitionTo(KnowledgeRecordStatus.Implemented);

        record.TransitionTo(KnowledgeRecordStatus.ReviewedUser);

        Assert.Equal(KnowledgeRecordStatus.ReviewedUser, record.Status);
    }

    [Fact]
    public void TransitionTo_ReviewedUserToDone_Succeeds()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        record.TransitionTo(KnowledgeRecordStatus.Implemented);
        record.TransitionTo(KnowledgeRecordStatus.ReviewedUser);

        record.TransitionTo(KnowledgeRecordStatus.Done);

        Assert.Equal(KnowledgeRecordStatus.Done, record.Status);
    }

    [Fact]
    public void TransitionTo_InFixToImplemented_Succeeds()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);
        record.TransitionTo(KnowledgeRecordStatus.InImplementation);
        record.TransitionTo(KnowledgeRecordStatus.Implemented);
        record.TransitionTo(KnowledgeRecordStatus.ReviewedAgent);
        record.TransitionTo(KnowledgeRecordStatus.InFix);

        record.TransitionTo(KnowledgeRecordStatus.Implemented);

        Assert.Equal(KnowledgeRecordStatus.Implemented, record.Status);
    }

    [Fact]
    public void TransitionTo_BacklogToDone_ThrowsDomainValidationException()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);

        Assert.Throws<DomainValidationException>(() => record.TransitionTo(KnowledgeRecordStatus.Done));
    }

    [Fact]
    public void TransitionTo_BacklogToImplemented_ThrowsDomainValidationException()
    {
        var record = KnowledgeRecord.Create(Guid.NewGuid(), "Story", "Desc", KnowledgeRecordType.Story, null);

        Assert.Throws<DomainValidationException>(() => record.TransitionTo(KnowledgeRecordStatus.Implemented));
    }

    [Fact]
    public void Reconstitute_RestoresAllProperties()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        var record = KnowledgeRecord.Reconstitute(
            id, projectId, "Epic Title", "Description", KnowledgeRecordStatus.InImplementation,
            KnowledgeRecordType.Epic, parentId, 1, createdAt, updatedAt);

        Assert.Equal(id, record.Id);
        Assert.Equal(projectId, record.ProjectId);
        Assert.Equal(KnowledgeRecordStatus.InImplementation, record.Status);
        Assert.Equal(parentId, record.ParentId);
        Assert.Equal(1, record.FormatVersion);
    }
}
