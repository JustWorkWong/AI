using System.IO;
using Xunit;

namespace Architecture.Tests;

public sealed class AppHostProjectTests
{
    [Fact]
    public void AppHost_project_should_reference_kubernetes_hosting()
    {
        var projectText = File.ReadAllText(@"D:\AI\src\Wms.AppHost\Wms.AppHost.csproj");

        Assert.Contains("Aspire.AppHost.Sdk", projectText);
        Assert.Contains("Aspire.Hosting.Kubernetes", projectText);
    }
}
