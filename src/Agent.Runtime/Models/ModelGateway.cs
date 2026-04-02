namespace Agent.Runtime.Models;

public interface IModelGateway
{
    ModelProfile? GetProfile(string profileCode);

    IReadOnlyList<ModelProfile> GetProfiles();
}

public sealed class ModelGateway(IConfiguration configuration) : IModelGateway
{
    private readonly IReadOnlyDictionary<string, ModelProfile> _profiles = LoadProfiles(configuration);

    public ModelProfile? GetProfile(string profileCode)
    {
        return _profiles.TryGetValue(profileCode, out var profile) ? profile : null;
    }

    public IReadOnlyList<ModelProfile> GetProfiles()
    {
        return _profiles.Values
            .OrderBy(x => x.ProfileCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyDictionary<string, ModelProfile> LoadProfiles(IConfiguration configuration)
    {
        var modelsSection = configuration.GetSection("Models");
        var profiles = new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in modelsSection.GetChildren())
        {
            var profile = new ModelProfile
            {
                ProfileCode = child.Key,
                Provider = child["Provider"] ?? "unknown",
                Endpoint = child["Endpoint"] ?? string.Empty,
                Model = child["Model"] ?? string.Empty,
                SupportsTools = !bool.TryParse(child["SupportsTools"], out var supportsTools) || supportsTools,
                SupportsStreaming = !bool.TryParse(child["SupportsStreaming"], out var supportsStreaming) || supportsStreaming
            };

            profiles[profile.ProfileCode] = profile;
        }

        if (profiles.Count > 0)
        {
            return profiles;
        }

        return new Dictionary<string, ModelProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = new()
            {
                ProfileCode = "default",
                Provider = "bailian",
                Endpoint = string.Empty,
                Model = "qwen-plus",
                SupportsTools = true,
                SupportsStreaming = true
            }
        };
    }
}
