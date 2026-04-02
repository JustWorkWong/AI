namespace Architecture.Tests;

public sealed class RepositoryConventionsTests
{
    [Fact]
    public void Repo_should_contain_publish_script_for_aspire_k8s()
    {
        Assert.True(File.Exists(@"D:\AI\build\publish-k8s.ps1"));
    }

    [Fact]
    public void Vite_proxy_should_target_ops_bff_development_port()
    {
        var viteConfig = File.ReadAllText(@"D:\AI\web\wms-web\vite.config.ts");

        Assert.Contains("http://localhost:5216", viteConfig);
    }
}
