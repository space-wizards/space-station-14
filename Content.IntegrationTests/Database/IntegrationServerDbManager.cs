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
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Database
{
    public class IntegrationServerDbManager : IServerDbManager
    {
        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly IResourceManager _resources = default!;

        private static readonly string Engine;
        private static readonly ServerDbBase Db;

        static IntegrationServerDbManager()
        {
            Engine = "sqlite";
            var options = CreateSqliteOptions();
            Db = new ServerDbSqlite(options);
        }

        public void Init()
        {
            var engine = _configuration.GetCVar(CCVars.DatabaseEngine).ToLower();

            if (engine != Engine)
            {
                throw new InvalidDataException($"Requested engine {engine} does not match used engine {Engine}.");
            }

            var inMemory = _resources.UserData.RootDir == null;

            if (!inMemory)
            {
                throw new InvalidDataException($"Requested a not inMemory database for an integration test.");
            }
        }

        private static DbContextOptions<ServerDbContext> CreateSqliteOptions()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();

            SqliteConnection connection = new("Data Source=:memory:");
            // When using an in-memory DB we have to open it manually
            // so EFCore doesn't open, close and wipe it.
            connection.Open();

            builder.UseSqlite(connection);
            return builder.Options;
        }

        public Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            return Db.InitPrefsAsync(userId, defaultProfile);
        }

        public Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            return Db.SaveSelectedCharacterIndexAsync(userId, index);
        }

        public Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            return Db.SaveCharacterSlotAsync(userId, profile, slot);
        }

        public Task DeleteSlotAndSetSelectedIndex(NetUserId userId, int deleteSlot, int newSlot)
        {
            return Db.DeleteSlotAndSetSelectedIndex(userId, deleteSlot, newSlot);
        }

        public Task SaveAdminOOCColorAsync(NetUserId userId, Color color)
        {
            return Db.SaveAdminOOCColorAsync(userId, color);
        }

        public Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            return Db.GetPlayerPreferencesAsync(userId);
        }

        public Task AssignUserIdAsync(string name, NetUserId userId)
        {
            return Db.AssignUserIdAsync(name, userId);
        }

        public Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            return Db.GetAssignedUserIdAsync(name);
        }

        public Task<ServerBanDef?> GetServerBanAsync(int id)
        {
            return Db.GetServerBanAsync(id);
        }

        public Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId)
        {
            return Db.GetServerBanAsync(address, userId);
        }

        public Task<List<ServerBanDef>> GetServerBansAsync(IPAddress? address, NetUserId? userId)
        {
            return Db.GetServerBansAsync(address, userId);
        }

        public Task AddServerBanAsync(ServerBanDef serverBan)
        {
            return Db.AddServerBanAsync(serverBan);
        }

        public Task AddServerUnbanAsync(ServerUnbanDef serverUnban)
        {
            return Db.AddServerUnbanAsync(serverUnban);
        }

        public Task UpdatePlayerRecordAsync(NetUserId userId, string userName, IPAddress address)
        {
            return Db.UpdatePlayerRecord(userId, userName, address);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserName(string userName, CancellationToken cancel = default)
        {
            return Db.GetPlayerRecordByUserName(userName, cancel);
        }

        public Task<PlayerRecord?> GetPlayerRecordByUserId(NetUserId userId, CancellationToken cancel = default)
        {
            return Db.GetPlayerRecordByUserId(userId, cancel);
        }

        public Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address)
        {
            return Db.AddConnectionLogAsync(userId, userName, address);
        }

        public Task<Admin?> GetAdminDataForAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return Db.GetAdminDataForAsync(userId, cancel);
        }

        public Task<AdminRank?> GetAdminRankAsync(int id, CancellationToken cancel = default)
        {
            return Db.GetAdminRankDataForAsync(id, cancel);
        }

        public Task<((Admin, string? lastUserName)[] admins, AdminRank[])> GetAllAdminAndRanksAsync(
            CancellationToken cancel = default)
        {
            return Db.GetAllAdminAndRanksAsync(cancel);
        }

        public Task RemoveAdminAsync(NetUserId userId, CancellationToken cancel = default)
        {
            return Db.RemoveAdminAsync(userId, cancel);
        }

        public Task AddAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return Db.AddAdminAsync(admin, cancel);
        }

        public Task UpdateAdminAsync(Admin admin, CancellationToken cancel = default)
        {
            return Db.UpdateAdminAsync(admin, cancel);
        }

        public Task RemoveAdminRankAsync(int rankId, CancellationToken cancel = default)
        {
            return Db.RemoveAdminRankAsync(rankId, cancel);
        }

        public Task AddAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return Db.AddAdminRankAsync(rank, cancel);
        }

        public Task UpdateAdminRankAsync(AdminRank rank, CancellationToken cancel = default)
        {
            return Db.UpdateAdminRankAsync(rank, cancel);
        }
    }
}
