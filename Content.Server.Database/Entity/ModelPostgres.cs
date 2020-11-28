namespace Content.Server.Database.Entity
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Net;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;

    public sealed class PostgresServerDbContext : ServerDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            //DO NOT COMMIT if (!options.IsConfigured)
            //    options.UseNpgsql("dummy connection string");

            options.ReplaceService<IRelationalTypeMappingSource, CustomNpgsqlTypeMappingSource>();
        }

        public PostgresServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }
    }
}
