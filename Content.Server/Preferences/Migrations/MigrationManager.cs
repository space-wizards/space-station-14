using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using Robust.Shared.Log;

namespace Content.Server.Preferences.Migrations
{
    /// <summary>
    /// Ensures database schemas are up to date.
    /// </summary>
    public static class MigrationManager
    {
        /// <summary>
        /// Ensures the database schema for the given connection string is up to date.
        /// </summary>
        public static void PerformUpgrade(string connectionString)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                EnsureSchemaVersionTableExists(connection);
                foreach (var migrationToRun in MigrationsToRun(connection))
                {
                    Logger.InfoS("db", "Running migration {0}", migrationToRun.Id);
                    migrationToRun.Run(connection);
                }
            }
        }

        /// <summary>
        /// Generated for each SQL file found.
        /// </summary>
        private class Migration
        {
            public readonly string Id;
            private readonly string _sql;

            public Migration(string id, string sql)
            {
                Id = id;
                _sql = sql;
            }

            /// <summary>
            /// Executes the query in <see cref="_sql"/> and logs this in the SchemaVersion table.
            /// </summary>
            public void Run(IDbConnection connection)
            {
                connection.Execute(_sql);
                InsertMigrationLog(connection, Id);
            }
        }

        private const string InsertMigrationLogQuery =
            @"INSERT INTO SchemaVersion (Id) VALUES (@Id)";
        /// <summary>
        /// Inserts a <see cref="MigrationLog"/> in the SchemaVersion table.
        /// </summary>
        private static void InsertMigrationLog(IDbConnection connection, string id)
        {
            Logger.InfoS("db", "Completing migration {0}", id);
            connection.Execute(InsertMigrationLogQuery, new {Id = id});
        }

        /// <summary>
        /// An entry in the SchemaVersion table.
        /// </summary>
        [UsedImplicitly]
        private class MigrationLog
        {
            public string Id;
            public string Timestamp;
        }

        private const string GetRanMigrationsQuery =
            @"SELECT Id, Timestamp FROM SchemaVersion ORDER BY Id COLLATE NOCASE";
        /// <summary>
        /// Fetches a collection of <see cref="MigrationLog"/> from the SchemaVersion table and returns it.
        /// </summary>
        private static IEnumerable<MigrationLog> RanMigrations(IDbConnection connection)
        {
            return connection.Query<MigrationLog>(GetRanMigrationsQuery);
        }

        /// <summary>
        /// Finds all available migrations, returns those that haven't been run yet.
        /// </summary>
        private static List<Migration> MigrationsToRun(IDbConnection connection)
        {
            var discoveredMigrations = DiscoverMigrations(connection);
            if (discoveredMigrations.Count == 0)
            {
                // No migrations found.
                return null;
            }

            var ranMigrations = RanMigrations(connection);

            // Filter out migrations that have already been executed
            discoveredMigrations
                .RemoveAll(migration => ranMigrations.Any(ranMigration => migration.Id == ranMigration.Id));
            return discoveredMigrations;
        }

        /// <summary>
        /// Given an embedded resource's full path returns its contents as a string.
        /// </summary>
        private static string ResourceAssemblyToString(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Searches the current assembly for SQL migration files.
        /// TODO: Filter by subfolder so that different databases use different sets of migrations.
        /// </summary>
        [NotNull]
        private static List<Migration> DiscoverMigrations(IDbConnection connection)
        {
            var results = new List<Migration>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var sqlResourceName in assembly
                .GetManifestResourceNames()
                .Where(IsValidMigrationFileName))
            {
                var splitName = sqlResourceName.Split('.');
                // The second to last string in the list is the actual file name without the final ".sql"
                var migrationId = splitName[splitName.Length - 2];
                var sqlContents = ResourceAssemblyToString(sqlResourceName);
                results.Add(new Migration(migrationId, sqlContents));
            }

            return results;
        }

        /// <summary>
        /// A valid file name is "000_Initial.sql". A dot (from the path, not to be included in the filename itself),
        /// three digits, a mandatory underscore, any number of characters, a mandatory ".sql".
        /// </summary>
        private static bool IsValidMigrationFileName(string name)
        {
            return Regex.IsMatch(name, @"\.\d\d\d_[a-zA-Z]+\.sql$");
        }

        private const string EnsureSchemaVersionTableExistsQuery =
            @"CREATE TABLE IF NOT EXISTS SchemaVersion (
              Id TEXT NOT NULL UNIQUE,
              Timestamp TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
              )";

        /// <summary>
        /// Creates the SchemaVersion table if it doesn't exist.
        /// </summary>
        private static void EnsureSchemaVersionTableExists(IDbConnection connection)
        {
            connection.Execute(EnsureSchemaVersionTableExistsQuery);
        }
    }
}
