using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
// ReSharper disable UnusedType.Global

namespace Content.Server.Database;

public sealed class DesignTimeContextFactoryPostgres : IDesignTimeDbContextFactory<PostgresServerDbContext>
{
    public PostgresServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresServerDbContext>();
        optionsBuilder.UseNpgsql(args[0]);
        return new PostgresServerDbContext(optionsBuilder.Options);
    }
}

public sealed class DesignTimeContextFactorySqlite : IDesignTimeDbContextFactory<SqliteServerDbContext>
{
    public SqliteServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        optionsBuilder.UseSqlite(args[0]);
        return new SqliteServerDbContext(optionsBuilder.Options);
    }
}
