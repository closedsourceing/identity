using Microsoft.EntityFrameworkCore;

namespace Identity.Server.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQLite does not support schemas; only set the default schema for relational
        // providers that support it (e.g. PostgreSQL).
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            builder.HasDefaultSchema("identity");
        }

        // Configure OpenIddict entities.
        builder.UseOpenIddict();
    }
}
