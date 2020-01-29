using System;
using System.IO;
using Content.Server.Interfaces;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;

namespace Content.Server.Database
{
    public class DatabaseManager : IDatabaseManager
    {
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configuration;
        [Dependency] private readonly IResourceManager _resourceManager;
#pragma warning restore 649
        public IDatabaseConfiguration DbConfig { get; private set; }

        public void Initialize()
        {
            _configuration.RegisterCVar("database.engine", "sqlite");
            _configuration.RegisterCVar("database.sqlite_dbpath", "ss14.db");
            _configuration.RegisterCVar("database.pg_host", "localhost");
            _configuration.RegisterCVar("database.pg_port", 5432);
            _configuration.RegisterCVar("database.pg_database", "ss14");
            _configuration.RegisterCVar("database.pg_username", string.Empty);
            _configuration.RegisterCVar("database.pg_password", string.Empty);

            var engine = _configuration.GetCVar<string>("database.engine").ToLower();
            switch (engine)
            {
                case "sqlite":
                    var configPreferencesDbPath = _configuration.GetCVar<string>("database.sqlite_dbpath");
                    var finalPreferencesDbPath =
                        Path.Combine(_resourceManager.UserData.RootDir, configPreferencesDbPath);
                    DbConfig = new SqliteConfiguration(
                        finalPreferencesDbPath
                    );
                    break;
                case "postgres":
                    DbConfig = new PostgresConfiguration(
                        _configuration.GetCVar<string>("database.pg_host"),
                        _configuration.GetCVar<int>("database.pg_port"),
                        _configuration.GetCVar<string>("database.pg_database"),
                        _configuration.GetCVar<string>("database.pg_username"),
                        _configuration.GetCVar<string>("database.pg_password")
                    );
                    break;
                default:
                    throw new NotImplementedException("Unknown database engine {engine}.");
            }
        }
    }
}
