using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Identity.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Server.Tests;

/// <summary>
/// Web application factory for integration tests. Uses SQLite in-memory database
/// so tests run without a PostgreSQL server.
/// </summary>
public sealed class IdentityServerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Shared, persistent SQLite connection kept open so the in-memory database
    // survives across multiple requests during the test run.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    /// <summary>Test client ID pre-seeded for integration tests.</summary>
    public const string TestClientId = "integration-test-client";

    /// <summary>Test redirect URI registered for the test client.</summary>
    public const string TestRedirectUri = "https://localhost/callback";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Tell Program.cs to use the SQLite branch.
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove the SQLite DbContext options that Program.cs registered
            // (it uses a separate connection string; we want to reuse our shared connection).
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Register the DbContext using the shared in-memory connection.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict();
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
    }

    /// <summary>
    /// Seeds a test application (client) that authorization endpoint tests can use.
    /// Call this from test constructors that need a registered client.
    /// </summary>
    public async Task SeedTestClientAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(TestClientId) is not null)
            return;

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = TestClientId,
            DisplayName = "Integration Test Client",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { new Uri(TestRedirectUri) },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                $"{Permissions.Prefixes.Scope}{Scopes.OpenId}",
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                $"{Permissions.Prefixes.Scope}{Scopes.OfflineAccess}"
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }
}
