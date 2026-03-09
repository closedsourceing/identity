using Identity.Server.Data;
using Identity.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────

// Support SQLite for testing ("Test" environment) and PostgreSQL for all others.
// The test factory sets ASPNETCORE_ENVIRONMENT=Test.
if (builder.Environment.IsEnvironment("Test"))
{
    // In the Test environment, the factory provides a pre-opened SQLite connection
    // via the "Sqlite:Connection" service. Here we just register a placeholder;
    // the factory replaces the DbContext registration entirely.
    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=:memory:";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(sqliteConnection);
        options.UseOpenIddict();
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is required. " +
            "Set it via the ConnectionStrings__DefaultConnection environment variable or user secrets.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
        });

        // Register the entity sets required by OpenIddict.
        options.UseOpenIddict();
    });
}

// ─── Authentication ───────────────────────────────────────────────────────────

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// ─── OpenIddict ───────────────────────────────────────────────────────────────

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options
            .SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetUserInfoEndpointUris("/connect/userinfo")
            .SetEndSessionEndpointUris("/connect/endsession")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetRevocationEndpointUris("/connect/revoke");

        // Supported flows.
        options
            .AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow();

        // PKCE is required for authorization code flow.
        options.RequireProofKeyForCodeExchange();

        // Token lifetimes (configurable via appsettings).
        var tokenOptions = builder.Configuration.GetSection("OpenIddict:TokenLifetimes");
        options.SetAccessTokenLifetime(
            TimeSpan.FromMinutes(tokenOptions.GetValue("AccessTokenMinutes", 60)));
        options.SetRefreshTokenLifetime(
            TimeSpan.FromDays(tokenOptions.GetValue("RefreshTokenDays", 14)));
        options.SetAuthorizationCodeLifetime(
            TimeSpan.FromMinutes(tokenOptions.GetValue("AuthorizationCodeMinutes", 5)));
        options.SetIdentityTokenLifetime(
            TimeSpan.FromMinutes(tokenOptions.GetValue("IdentityTokenMinutes", 60)));

        // Scopes.
        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess);

        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test"))
        {
            // Use ephemeral signing/encryption keys in dev/test (no persistent key store needed).
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();

            // Disable HTTPS requirement for local dev and automated tests.
            options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough()
                .DisableTransportSecurityRequirement();
        }
        else
        {
            // Production: use ephemeral key (replace with persistent signing key in real deployment).
            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey();

            options.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough();
        }
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// ─── MVC ─────────────────────────────────────────────────────────────────────

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

// ─── Seed worker ─────────────────────────────────────────────────────────────

builder.Services.AddHostedService<ClientSeedWorker>();

// ─── Logging (structured) ─────────────────────────────────────────────────────

builder.Logging.AddConsole();

// ─── Build ────────────────────────────────────────────────────────────────────

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new { Status = "Identity Server running" }));
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.Run();

// Make Program accessible for WebApplicationFactory in tests.
public partial class Program { }
