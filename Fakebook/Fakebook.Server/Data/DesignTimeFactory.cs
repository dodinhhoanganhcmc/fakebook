using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fakebook.Server.Data;

// Used by `dotnet ef` at design time to create a DbContext without booting the AppHost.
public class FakebookDbContextFactory : IDesignTimeDbContextFactory<FakebookDbContext>
{
    public FakebookDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__fakebookdb")
                   ?? "Host=localhost;Port=5432;Database=fakebook_db;Username=app_backend;Password=admin";

        var options = new DbContextOptionsBuilder<FakebookDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new FakebookDbContext(options);
    }
}
