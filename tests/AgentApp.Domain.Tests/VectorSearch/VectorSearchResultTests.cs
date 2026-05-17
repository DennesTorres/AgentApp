using AgentApp.Domain.VectorSearch;

namespace AgentApp.Domain.Tests.VectorSearch;

public class VectorSearchResultTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var result = VectorSearchResult.Create("sess-1", "React hooks summary", 0.95f);
        Assert.Equal("sess-1", result.Id);
        Assert.Equal("React hooks summary", result.Content);
        Assert.Equal(0.95f, result.Score);
    }
}
