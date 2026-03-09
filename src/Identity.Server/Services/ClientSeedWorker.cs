using Identity.Server.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Server.Services;

/// <summary>
/// Background worker that idempotently seeds OpenIddict applications and scopes on startup.
/// </summary>
public sealed class ClientSeedWorker(
    IServiceProvider serviceProvider,
    ILogger<ClientSeedWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // For SQLite (used in tests), use EnsureCreated because the database has no
        // migration bundle. For other providers (PostgreSQL), run EF migrations.
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        await SeedScopesAsync(scope.ServiceProvider, cancellationToken);
        await SeedClientsAsync(scope.ServiceProvider, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedScopesAsync(IServiceProvider services, CancellationToken ct)
    {
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        await EnsureScopeAsync(scopeManager,
            name: Scopes.Email,
            displayName: "Email address",
            ct: ct);

        await EnsureScopeAsync(scopeManager,
            name: Scopes.Profile,
            displayName: "User profile",
            ct: ct);

        await EnsureScopeAsync(scopeManager,
            name: Scopes.Roles,
            displayName: "User roles",
            ct: ct);

        logger.LogInformation("OpenIddict scopes seeded successfully.");
    }

    private static async Task EnsureScopeAsync(
        IOpenIddictScopeManager manager, string name, string displayName, CancellationToken ct)
    {
        if (await manager.FindByNameAsync(name, ct) is null)
        {
            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = displayName
            }, ct);
        }
    }

    private async Task SeedClientsAsync(IServiceProvider services, CancellationToken ct)
    {
        var appManager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var config = services.GetRequiredService<IConfiguration>();

        var clients = config
            .GetSection(ClientSeedOptions.SectionName)
            .Get<List<ClientSeedOptions>>() ?? [];

        foreach (var client in clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                logger.LogWarning("Skipping seeded client with empty ClientId.");
                continue;
            }

            var existing = await appManager.FindByClientIdAsync(client.ClientId, ct);
            if (existing is not null)
            {
                logger.LogDebug("OpenIddict application '{ClientId}' already exists — skipping.", client.ClientId);
                continue;
            }

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                DisplayName = client.DisplayName ?? client.ClientId,
                ClientSecret = string.IsNullOrWhiteSpace(client.ClientSecret) ? null : client.ClientSecret,
                ConsentType = ParseConsentType(client.ConsentType),
                ClientType = ParseClientType(client.ClientType)
            };

            foreach (var uri in client.RedirectUris)
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                    descriptor.RedirectUris.Add(parsed);
                else
                    logger.LogWarning("Invalid redirect URI '{Uri}' for client '{ClientId}' — skipping.", uri, client.ClientId);
            }

            foreach (var uri in client.PostLogoutRedirectUris)
            {
                if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
                    descriptor.PostLogoutRedirectUris.Add(parsed);
                else
                    logger.LogWarning("Invalid post-logout redirect URI '{Uri}' for client '{ClientId}' — skipping.", uri, client.ClientId);
            }

            foreach (var permission in client.Permissions)
                descriptor.Permissions.Add(permission);

            await appManager.CreateAsync(descriptor, ct);
            logger.LogInformation("OpenIddict application '{ClientId}' seeded.", client.ClientId);
        }
    }

    private static string ParseConsentType(string value) => value.ToLowerInvariant() switch
    {
        "explicit" => ConsentTypes.Explicit,
        "external" => ConsentTypes.External,
        "systematic" => ConsentTypes.Systematic,
        _ => ConsentTypes.Implicit
    };

    private static string ParseClientType(string value) => value.ToLowerInvariant() switch
    {
        "public" => ClientTypes.Public,
        _ => ClientTypes.Confidential
    };
}
