namespace Identity.Server.Services;

/// <summary>
/// Configuration for a seeded OpenIddict application (OAuth client).
/// </summary>
public sealed class ClientSeedOptions
{
    public const string SectionName = "OpenIddict:Clients";

    public string ClientId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    /// <summary>
    /// Client secret in plain text. OpenIddict hashes it internally during client creation.
    /// Leave null or empty for public clients. Provide via environment variable or user secrets —
    /// never commit plain-text secrets to source control.
    /// </summary>
    public string? ClientSecret { get; set; }

    public string ConsentType { get; set; } = "implicit";
    public string ClientType { get; set; } = "confidential";
    public IList<string> RedirectUris { get; set; } = [];
    public IList<string> PostLogoutRedirectUris { get; set; } = [];
    public IList<string> Permissions { get; set; } = [];
}
