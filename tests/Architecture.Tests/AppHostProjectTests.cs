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

    [Fact]
    public void AppHost_should_define_launch_profile_with_dashboard_environment()
    {
        var launchSettingsPath = @"D:\AI\src\Wms.AppHost\Properties\launchSettings.json";

        Assert.True(File.Exists(launchSettingsPath));

        var launchSettings = File.ReadAllText(launchSettingsPath);

        Assert.Contains("ASPNETCORE_URLS", launchSettings);
        Assert.Contains("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", launchSettings);
        Assert.Contains("ASPIRE_ALLOW_UNSECURED_TRANSPORT", launchSettings);
    }

    [Fact]
    public void AppHost_should_persist_stateful_infrastructure_with_named_volumes()
    {
        var appHostText = File.ReadAllText(@"D:\AI\src\Wms.AppHost\AppHost.cs");

        Assert.Contains("AddParameter(", appHostText);
        Assert.Contains("\"postgres-password\"", appHostText);
        Assert.Contains(".WithPassword(postgresPassword)", appHostText);
        Assert.Contains("WithDataVolume(\"wms-postgres-data\")", appHostText);
        Assert.Contains("WithDataVolume(\"wms-redis-data\")", appHostText);
        Assert.Contains("WithDataVolume(\"wms-rabbitmq-data\")", appHostText);
        Assert.Contains("WithVolume(\"wms-minio-data\", \"/data\")", appHostText);
    }
}
