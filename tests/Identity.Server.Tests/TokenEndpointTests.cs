using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace Identity.Server.Tests;

/// <summary>
/// Integration tests for OAuth 2.0 / OIDC token endpoint flows.
/// </summary>
public sealed class TokenEndpointTests(IdentityServerFactory factory)
    : IClassFixture<IdentityServerFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Token_WithInvalidGrantType_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "implicit",
            ["client_id"] = "some-client"
        });

        var response = await _client.PostAsync("/connect/token", content);

        // OpenIddict returns 400 for unsupported grant type.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.Equal("unsupported_grant_type", error);
    }

    [Fact]
    public async Task Token_ClientCredentials_WithUnknownClient_ReturnsUnauthorized()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "nonexistent-client",
            ["client_secret"] = "wrong-secret",
            ["scope"] = "openid"
        });

        var response = await _client.PostAsync("/connect/token", content);

        // Should be 400 (invalid_client) not 500.
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 400 or 401 but got {response.StatusCode}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out _),
            "Error response must include 'error' field.");
    }

    [Fact]
    public async Task Token_AuthCode_WithInvalidCode_ReturnsError()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = "test-client",
            ["code"] = "invalid-code-value",
            ["redirect_uri"] = "https://localhost/callback",
            ["code_verifier"] = "invalid-verifier"
        });

        var response = await _client.PostAsync("/connect/token", content);

        // OpenIddict returns 400 or 401 for invalid authorization codes/client auth.
        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized,
            $"Expected 400 or 401 but got {response.StatusCode}");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("error", out _),
            "Error response must include 'error' field.");
    }

    [Fact]
    public async Task Token_MissingGrantType_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = "test-client"
        });

        var response = await _client.PostAsync("/connect/token", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
