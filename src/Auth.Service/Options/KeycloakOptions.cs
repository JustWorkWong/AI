namespace Auth.Service.Options;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; init; } = "https://keycloak.local/realms/wms";

    public string Audience { get; init; } = "wms-ai-platform";

    public bool RequireHttpsMetadata { get; init; } = true;
}
