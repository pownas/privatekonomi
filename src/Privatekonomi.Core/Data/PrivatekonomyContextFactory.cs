using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Privatekonomi.Core.Data;

/// <summary>
/// Design-time factory for creating PrivatekonomyContext for EF migrations
/// </summary>
public class PrivatekonomyContextFactory : IDesignTimeDbContextFactory<PrivatekonomyContext>
{
    private static readonly ServiceProvider IdentityOptionsProvider = new ServiceCollection()
        .Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
        .BuildServiceProvider();

    public PrivatekonomyContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PrivatekonomyContext>();
        
        // Use SQLite for migrations
        optionsBuilder
            .UseSqlite("Data Source=privatekonomi.db")
            .UseApplicationServiceProvider(IdentityOptionsProvider);
        
        return new PrivatekonomyContext(optionsBuilder.Options);
    }
}
