namespace Architecture.Tests;

public sealed class RepositoryConventionsTests
{
    [Fact]
    public void Repo_should_contain_publish_script_for_aspire_k8s()
    {
        Assert.True(File.Exists(@"D:\AI\build\publish-k8s.ps1"));
    }
}
