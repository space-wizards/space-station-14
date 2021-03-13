#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Database
{
    public class IntegrationServerDbManager : IServerDbManager
    {
        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly IResourceManager _resources = default!;
        [Dependency] private readonly ILogManager _logger = default!;

        private static string _engine = default!;
        private static ServerDbBase _db = default!;
        private static readonly object _dbLock = new();

        public void Init()
        {
            var engine = _configuration.GetCVar(CCVars.DatabaseEngine).ToLower();

            if (_engine != null && engine != _engine)
            {
                throw new InvalidDataException($"Unknown database engine {engine}.");
            }

            _engine = engine;

            if (_db == null!)
            {
                lock (_dbLock)
                {
                    if (_db == null!)
                    {
                        switch (engine)
                        {
                            case "sqlite":
                                var options = CreateSqliteOptions();
                                _db = new ServerDbSqlite(options);
                                break;
                            default:
                                throw new InvalidDataException($"Unknown database engine {engine}.");
                        }
                    }
                }
            }
        }

        private DbContextOptions<ServerDbContext> CreateSqliteOptions()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();

            var configPreferencesDbPath = _configuration.GetCVar(CCVars.DatabaseSqliteDbPath);
            var inMemory = _resources.UserData.RootDir == null;

            SqliteConnection connection;
            if (!inMemory)
            {
                var finalPreferencesDbPath = Path.Combine(_resources.UserData.RootDir!, configPreferencesDbPath);
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
            return builder.Options;
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

        public Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            return _db.SaveAdminOOCColorAsync(userId, color);
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

        public Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            return _db.GetServerBanAsync(id);
        }

        public Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            return _db.GetServerBanAsync(address, userId);
        }

        public Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address, NetUserId? userId)
        {
            return _db.GetServerBansAsync(address, userId);
        }

        public Task AddServerBanAsync(ServerBanDef serverBan)
        {
            return _db.AddServerBanAsync(serverBan);
        }

        public Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            return _db.AddServerUnbanAsync(serverUnban);
        }

        public Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address)
        {
            return _db.UpdatePlayerRecord(userId, userName, address);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default)
        {
            return _db.GetPlayerRecordByUserName(userName, cancel);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.GetPlayerRecordByUserId(userId, cancel);
        }

        public Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            return _db.AddConnectionLogAsync(userId, userName, address);
        }

        public Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.GetAdminDataForAsync(userId, cancel);
        }

        public Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default)
        {
            return _db.GetAdminRankDataForAsync(id, cancel);
        }

        public Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default)
        {
            return _db.GetAllAdminAndRanksAsync(cancel);
        }

        public Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return _db.RemoveAdminAsync(userId, cancel);
        }

        public Task AddAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return _db.AddAdminAsync(admin, cancel);
        }

        public Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return _db.UpdateAdminAsync(admin, cancel);
        }

        public Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default)
        {
            return _db.RemoveAdminRankAsync(rankId, cancel);
        }

        public Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return _db.AddAdminRankAsync(rank, cancel);
        }

        public Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return _db.UpdateAdminRankAsync(rank, cancel);
        }
    }
}
