using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Commands;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class AddTests
{
    [Test]
    public async Task AddAndGetSingleLog()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
        var server = pairTracker.Pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var coordinates = testMap.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {guid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = sAdminLogSystem.CurrentRoundJson(new LogFilter
            {
                Search = guid.ToString()
            });

            await foreach (var json in logs)
            {
                var root = json.RootElement;

                // camelCased automatically
                Assert.That(root.TryGetProperty("entity", out _), Is.True);

                json.Dispose();

                return true;
            }

            return false;
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task AddAndGetUnformattedLog()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
        var server = pairTracker.Pair.Server;

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var coordinates = testMap.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}");
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            log = logs.First();
            return true;
        });

        var filter = new LogFilter
        {
            Round = sGamerTicker.RoundId,
            Search = log.Message,
            Types = new HashSet<LogType> {log.Type},
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.That(root.TryGetProperty("entity", out _), Is.True);
            Assert.That(root.TryGetProperty("guid", out _), Is.True);

            json.Dispose();
        }

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    [TestCase(500)]
    public async Task BulkAddLogs(int amount)
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
        var server = pairTracker.Pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var testMap = await PoolManager.CreateTestMap(pairTracker);
        var coordinates = testMap.GridCoords;
        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            for (var i = 0; i < amount; i++)
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
            }
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var messages = await sAdminLogSystem.CurrentRoundLogs();
            return messages.Count >= amount;
        });

        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task AddPlayerSessionLog()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        Guid playerGuid = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.ServerSessions.First();
            playerGuid = player.UserId;

            Assert.DoesNotThrow(() =>
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{player:Player} test log.");
            });
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs();
            if (logs.Count == 0)
            {
                return false;
            }

            Assert.That(logs.First().Players, Does.Contain(playerGuid));
            return true;
        });
        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task PreRoundAddAndGetSingle()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Dirty = true});
        var server = pairTracker.Pair.Server;

        var configManager = server.ResolveDependency<IConfigurationManager>();
        await server.WaitPost(() =>
        {
            configManager.SetCVar(CCVars.GameLobbyEnabled, true);
            var command = new RestartRoundNowCommand();
            command.Execute(null, string.Empty, Array.Empty<string>());
        });

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            sAdminLogSystem.Add(LogType.Unknown, $"test log: {guid}");
        });

        await server.WaitPost(() =>
        {
            sGamerTicker.StartRound(true);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            log = logs.First();
            return true;
        });

        var filter = new LogFilter
        {
            Round = sGamerTicker.RoundId,
            Search = log.Message,
            Types = new HashSet<LogType> {log.Type},
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.That(root.TryGetProperty("guid", out _), Is.True);

            json.Dispose();
        }
        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task DuplicatePlayerDoesNotThrowTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.ServerSessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player} {player} test log: {guid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            return true;
        });

        await pairTracker.CleanReturnAsync();
        Assert.Pass();
    }

    [Test]
    public async Task DuplicatePlayerIdDoesNotThrowTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient();
        var server = pairTracker.Pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.ServerSessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player:first} {player:second} test log: {guid}");
        });

        await PoolManager.WaitUntil(server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            if (logs.Count == 0)
            {
                return false;
            }

            return true;
        });

        await pairTracker.CleanReturnAsync();
        Assert.Pass();
    }
}
