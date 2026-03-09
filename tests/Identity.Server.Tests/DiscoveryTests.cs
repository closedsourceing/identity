using System.Net.Http.Json;
using System.Text.Json;

namespace Identity.Server.Tests;

/// <summary>
/// Integration tests for OIDC discovery and JWKS endpoints.
/// </summary>
public sealed class DiscoveryTests(IdentityServerFactory factory)
    : IClassFixture<IdentityServerFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Discovery_ReturnsOk()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Discovery_ContainsRequiredFields()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Required OIDC discovery fields
        Assert.True(json.TryGetProperty("issuer", out var issuer),
            "Discovery document must contain 'issuer'.");
        Assert.False(string.IsNullOrWhiteSpace(issuer.GetString()),
            "Issuer must not be empty.");

        Assert.True(json.TryGetProperty("authorization_endpoint", out _),
            "Discovery document must contain 'authorization_endpoint'.");

        Assert.True(json.TryGetProperty("token_endpoint", out _),
            "Discovery document must contain 'token_endpoint'.");

        Assert.True(json.TryGetProperty("jwks_uri", out _),
            "Discovery document must contain 'jwks_uri'.");

        Assert.True(json.TryGetProperty("response_types_supported", out _),
            "Discovery document must contain 'response_types_supported'.");

        Assert.True(json.TryGetProperty("subject_types_supported", out _),
            "Discovery document must contain 'subject_types_supported'.");

        Assert.True(json.TryGetProperty("id_token_signing_alg_values_supported", out _),
            "Discovery document must contain 'id_token_signing_alg_values_supported'.");
    }

    [Fact]
    public async Task Discovery_SupportsPkce()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("code_challenge_methods_supported", out var methods),
            "Discovery document must contain 'code_challenge_methods_supported'.");

        var methodArray = methods.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("S256", methodArray);
    }

    [Fact]
    public async Task Discovery_SupportsAuthCodeAndClientCredentials()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("grant_types_supported", out var grantTypes));

        var grants = grantTypes.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("authorization_code", grants);
        Assert.Contains("client_credentials", grants);
    }

    [Fact]
    public async Task Jwks_ReturnsOk()
    {
        // Get the JWKS URI from discovery first.
        var discoveryResponse = await _client.GetAsync("/.well-known/openid-configuration");
        var discovery = await discoveryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jwksUri = discovery.GetProperty("jwks_uri").GetString()!;

        // Strip the base address if absolute.
        var uri = new Uri(jwksUri);
        var response = await _client.GetAsync(uri.PathAndQuery);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Jwks_ContainsKeys()
    {
        var discoveryResponse = await _client.GetAsync("/.well-known/openid-configuration");
        var discovery = await discoveryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jwksUri = new Uri(discovery.GetProperty("jwks_uri").GetString()!);

        var response = await _client.GetAsync(jwksUri.PathAndQuery);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("keys", out var keys),
            "JWKS response must contain a 'keys' array.");
        Assert.True(keys.GetArrayLength() > 0, "JWKS must contain at least one key.");

        var firstKey = keys.EnumerateArray().First();
        Assert.True(firstKey.TryGetProperty("kty", out _), "Key must have 'kty'.");
        Assert.True(firstKey.TryGetProperty("use", out _), "Key must have 'use'.");
        Assert.True(firstKey.TryGetProperty("kid", out _), "Key must have 'kid'.");
    }
}
