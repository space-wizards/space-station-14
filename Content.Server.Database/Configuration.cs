using Microsoft.Data.Sqlite;
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
        private readonly string? _databaseFilePath;

        /// <param name="databaseFilePath">If null, an in-memory database is used.</param>
        public SqliteConfiguration(string? databaseFilePath)
        {
            _databaseFilePath = databaseFilePath;
        }

        public DbContextOptions<PreferencesDbContext> Options
        {
            get
            {
                var optionsBuilder = new DbContextOptionsBuilder<PreferencesDbContext>();
                SqliteConnection connection;
                if (_databaseFilePath != null)
                {
                    connection = new SqliteConnection($"Data Source={_databaseFilePath}");
                }
                else
                {
                    connection = new SqliteConnection("Data Source=:memory:");
                    // When using an in-memory DB we have to open it manually
                    // so EFCore doesn't open, close and wipe it.
                    connection.Open();
                }

                optionsBuilder.UseSqlite(connection);
                return optionsBuilder.Options;
            }
        }
    }
}
