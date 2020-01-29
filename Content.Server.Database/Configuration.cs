using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Content.Server.Database
{
    public interface IDatabaseConfiguration
    {
        DbContextOptions<PreferencesDbContext> Options { get; }
    }

    public class PostgresConfiguration : IDatabaseConfiguration
    {
        private readonly string _database;
        private readonly string _host;
        private readonly string _password;
        private readonly int _port;
        private readonly string _username;

        public PostgresConfiguration(string host,
            int port,
            string database,
            string username,
            string password)
        {
            _host = host;
            _port = port;
            _database = database;
            _username = username;
            _password = password;
        }

        public DbContextOptions<PreferencesDbContext> Options
        {
            get
            {
                var optionsBuilder = new DbContextOptionsBuilder<PreferencesDbContext>();
                var connectionString = new NpgsqlConnectionStringBuilder
                {
                    Host = _host,
                    Port = _port,
                    Database = _database,
                    Username = _username,
                    Password = _password
                }.ConnectionString;
                optionsBuilder.UseNpgsql(connectionString);
                return optionsBuilder.Options;
            }
        }
    }

    public class SqliteConfiguration : IDatabaseConfiguration
    {
        private readonly string _databaseFilePath;

        public SqliteConfiguration(string databaseFilePath)
        {
            _databaseFilePath = databaseFilePath;
        }

        public DbContextOptions<PreferencesDbContext> Options
        {
            get
            {
                var optionsBuilder = new DbContextOptionsBuilder<PreferencesDbContext>();
                optionsBuilder.UseSqlite($"Data Source={_databaseFilePath}");
                return optionsBuilder.Options;
            }
        }
    }
}
