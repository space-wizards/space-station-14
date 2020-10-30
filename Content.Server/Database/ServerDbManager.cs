﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Content.Shared;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
using LogLevel = Robust.Shared.Log.LogLevel;

#nullable enable

namespace Content.Server.Database
{
    public interface IServerDbManager
    {
        void Init();

        // Preferences
        Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile);
        Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index);
        Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot);
        // Single method for two operations for transaction.
        Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot);
        Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId);

        // Username assignment (for guest accounts, so they persist GUID)
        Task AssignUserIdAsync(string name, NetUserId userId);
        Task<NetUserId?> GetAssignedUserIdAsync(string name);

        // Ban stuff
        Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId);
        Task AddServerBanAsync(ServerBanDef serverBan);

        // Player records
        Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address);

        // Connection log
        Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address);

        // Admins
        Task<Admin?> GetAdminDataForAsync(NetUserId userId);
    }

    public sealed class ServerDbManager : IServerDbManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _res = default!;
        [Dependency] private readonly ILogManager _logMgr = default!;

        private ServerDbBase _db = default!;
        private LoggingProvider _msLogProvider = default!;
        private ILoggerFactory _msLoggerFactory = default!;


        public void Init()
        {
            _msLogProvider = new LoggingProvider(_logMgr);
            _msLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_msLogProvider);
            });

            var engine = _cfg.GetCVar(CCVars.DatabaseEngine).ToLower();
            switch (engine)
            {
                case "sqlite":
                    var options = CreateSqliteOptions();
                    _db = new ServerDbSqlite(options);
                    break;
                case "postgres":
                    options = CreatePostgresOptions();
                    _db = new ServerDbPostgres(options);
                    break;
                default:
                    throw new InvalidDataException("Unknown database engine {engine}.");
            }
        }

        public Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            return _db.InitPrefsAsync(userId, defaultProfile);
        }

        public Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            return _db.SaveSelectedCharacterIndexAsync(userId, index);
        }

        public Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            return _db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        public Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            return _db.DeleteSlotAndSetSelectedIndex(userId, deleteSlot, newSlot);
        }

        public Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            return _db.GetPlayerPreferencesAsync(userId);
        }

        public Task AssignUserIdAsync(string name, NetUserId userId)
        {
            return _db.AssignUserIdAsync(name, userId);
        }

        public Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            return _db.GetAssignedUserIdAsync(name);
        }

        public Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            return _db.GetServerBanAsync(address, userId);
        }

        public Task AddServerBanAsync(ServerBanDef serverBan)
        {
            return _db.AddServerBanAsync(serverBan);
        }

        public Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address)
        {
            return _db.UpdatePlayerRecord(userId, userName, address);
        }

        public Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            return _db.AddConnectionLogAsync(userId, userName, address);
        }

        public Task<Admin?> GetAdminDataForAsync(NetUserId userId)
        {
            return _db.GetAdminDataForAsync(userId);
        }

        private DbContextOptions<ServerDbContext> CreatePostgresOptions()
        {
            var host = _cfg.GetCVar(CCVars.DatabasePgHost);
            var port = _cfg.GetCVar(CCVars.DatabasePgPort);
            var db = _cfg.GetCVar(CCVars.DatabasePgDatabase);
            var user = _cfg.GetCVar(CCVars.DatabasePgUsername);
            var pass = _cfg.GetCVar(CCVars.DatabasePgPassword);

            var builder = new DbContextOptionsBuilder<ServerDbContext>();
            var connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = db,
                Username = user,
                Password = pass
            }.ConnectionString;
            builder.UseNpgsql(connectionString);
            SetupLogging(builder);
            return builder.Options;
        }

        private DbContextOptions<ServerDbContext> CreateSqliteOptions()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();

            var configPreferencesDbPath = _cfg.GetCVar(CCVars.DatabaseSqliteDbPath);
            var inMemory = _res.UserData.RootDir == null;

            SqliteConnection connection;
            if (!inMemory)
            {
                var finalPreferencesDbPath = Path.Combine(_res.UserData.RootDir!, configPreferencesDbPath);
                connection = new SqliteConnection($"Data Source={finalPreferencesDbPath}");
            }
            else
            {
                connection = new SqliteConnection("Data Source=:memory:");
                // When using an in-memory DB we have to open it manually
                // so EFCore doesn't open, close and wipe it.
                connection.Open();
            }

            builder.UseSqlite(connection);
            SetupLogging(builder);
            return builder.Options;
        }

        private void SetupLogging(DbContextOptionsBuilder<ServerDbContext> builder)
        {
            builder.UseLoggerFactory(_msLoggerFactory);
        }

        private sealed class LoggingProvider : ILoggerProvider
        {
            private readonly ILogManager _logManager;

            public LoggingProvider(ILogManager logManager)
            {
                _logManager = logManager;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new MSLogger(_logManager.GetSawmill("db.ef"));
            }
        }

        private sealed class MSLogger : ILogger
        {
            private readonly ISawmill _sawmill;

            public MSLogger(ISawmill sawmill)
            {
                _sawmill = sawmill;
            }

            public void Log<TState>(MSLogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var lvl = logLevel switch
                {
                    MSLogLevel.Trace => LogLevel.Debug,
                    MSLogLevel.Debug => LogLevel.Debug,
                    // EFCore feels the need to log individual DB commands as "Information" so I'm slapping debug on it.
                    MSLogLevel.Information => LogLevel.Debug,
                    MSLogLevel.Warning => LogLevel.Warning,
                    MSLogLevel.Error => LogLevel.Error,
                    MSLogLevel.Critical => LogLevel.Fatal,
                    MSLogLevel.None => LogLevel.Debug,
                    _ => LogLevel.Debug
                };

                _sawmill.Log(lvl, formatter(state, exception));
            }

            public bool IsEnabled(MSLogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                // TODO: this
                return null!;
            }
        }
    }
}
