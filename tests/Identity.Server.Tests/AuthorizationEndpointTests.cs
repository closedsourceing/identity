using System.Net;

namespace Identity.Server.Tests;

/// <summary>
/// Integration tests for the authorization endpoint.
/// </summary>
public sealed class AuthorizationEndpointTests(IdentityServerFactory factory)
    : IClassFixture<IdentityServerFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task Authorize_WithoutSession_RedirectsToLogin()
    {
        // Seed a known client so the authorization request is valid.
        await factory.SeedTestClientAsync();

        var redirectUri = Uri.EscapeDataString(IdentityServerFactory.TestRedirectUri);
        var codeChallenge = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

        var response = await _client.GetAsync(
            $"/connect/authorize?client_id={IdentityServerFactory.TestClientId}" +
            $"&response_type=code&redirect_uri={redirectUri}" +
            $"&scope=openid&code_challenge={codeChallenge}&code_challenge_method=S256");

        // Should redirect to the login page because the user is not authenticated.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authorize_WithUnknownClient_ReturnsError()
    {
        // An unknown client should result in an error response, not a redirect.
        var response = await _client.GetAsync(
            "/connect/authorize?client_id=nonexistent-client&response_type=code" +
            "&redirect_uri=https%3A%2F%2Flocalhost%2Fcallback&scope=openid");

        // OpenIddict returns a 400 for an unknown/invalid client.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Get_ReturnsLoginPage()
    {
        var response = await _client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content, StringComparison.OrdinalIgnoreCase);
    }
}
