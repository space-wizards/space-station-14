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
    public class MigrationManager
    {
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

        private class Migration
        {
            public readonly string Id;
            private readonly string _sql;

            public Migration(string id, string sql)
            {
                Id = id;
                _sql = sql;
            }

            public void Run(IDbConnection connection)
            {
                connection.Execute(_sql);
                InsertMigrationLog(connection, Id);
            }
        }

        private const string InsertMigrationLogQuery =
            @"INSERT INTO SchemaVersion (Id) VALUES (@Id)";
        private static void InsertMigrationLog(IDbConnection connection, string id)
        {
            Logger.InfoS("db", "Completing migration {0}", id);
            connection.Execute(InsertMigrationLogQuery, new {Id = id});
        }

        [UsedImplicitly]
        private class MigrationLog
        {
            public string Id;
            public string Timestamp;
        }

        private const string GetRanMigrationsQuery =
            @"SELECT Id, Timestamp FROM SchemaVersion ORDER BY Id COLLATE NOCASE";

        private static IEnumerable<MigrationLog> RanMigrations(IDbConnection connection)
        {
            return connection.Query<MigrationLog>(GetRanMigrationsQuery);
        }

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

        private static string ResourceAssemblyToString(string resourceName)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

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

        private static void EnsureSchemaVersionTableExists(IDbConnection connection)
        {
            var affected = connection.Execute(EnsureSchemaVersionTableExistsQuery);
            if(affected != 0)
                Logger.Info("db", "Created SchemaVersion table.");
        }
    }
}
