using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class AddTests
{
    public static PoolSettings LogTestSettings = new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [Test]
    public async Task AddAndGetSingleLog()
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;
        var sEntities = server.ResolveDependency<IEntityManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await pair.CreateTestMap();
        var coordinates = pair.TestMap.GridCoords;
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

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AddAndGetUnformattedLog()
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        var testMap = await pair.CreateTestMap();
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
            Types = new HashSet<LogType> { log.Type },
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.Multiple(() =>
            {
                Assert.That(root.TryGetProperty("entity", out _), Is.True);
                Assert.That(root.TryGetProperty("guid", out _), Is.True);
            });

            json.Dispose();
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    [TestCase(500)]
    public async Task BulkAddLogs(int amount)
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var testMap = await pair.CreateTestMap();
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

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AddPlayerSessionLog()
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();
        Guid playerGuid = default;

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.First();
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
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PreRoundAddAndGetSingle()
    {
        var setting = new PoolSettings
        {
            Dirty = true,
            InLobby = true,
            AdminLogsEnabled = true
        };

        await using var pair = await PoolManager.GetServerClient(setting);
        var server = pair.Server;

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
            Types = new HashSet<LogType> { log.Type },
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.That(root.TryGetProperty("guid", out _), Is.True);

            json.Dispose();
        }
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DuplicatePlayerDoesNotThrowTest()
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

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

        await pair.CleanReturnAsync();
        Assert.Pass();
    }

    [Test]
    public async Task DuplicatePlayerIdDoesNotThrowTest()
    {
        await using var pair = await PoolManager.GetServerClient(LogTestSettings);
        var server = pair.Server;

        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

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

        await pair.CleanReturnAsync();
        Assert.Pass();
    }
}
