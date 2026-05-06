#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
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
public sealed class AddTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        AdminLogsEnabled = true,
        DummyTicker = false,
        Connected = true
    };

    [Test]
    public async Task AddAndGetSingleLog()
    {
        var sEntities = Server.ResolveDependency<IEntityManager>();

        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await Pair.CreateTestMap();
        var coordinates = Pair.TestMap!.GridCoords;
        await Server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {guid}");
        });

        await PoolManager.WaitUntil(Server, async () =>
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
    }

    [Test]
    public async Task AddAndGetUnformattedLog()
    {
        var sDatabase = Server.ResolveDependency<IServerDbManager>();
        var sEntities = Server.ResolveDependency<IEntityManager>();
        var sSystems = Server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        var testMap = await Pair.CreateTestMap();
        var coordinates = testMap.GridCoords;
        await Server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}");
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(Server, async () =>
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
    }

    [Test]
    [TestCase(500)]
    public async Task BulkAddLogs(int amount)
    {
        var sEntities = Server.ResolveDependency<IEntityManager>();
        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();

        var testMap = await Pair.CreateTestMap();
        var coordinates = testMap.GridCoords;
        await Server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            for (var i = 0; i < amount; i++)
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
            }
        });

        await PoolManager.WaitUntil(Server, async () =>
        {
            var messages = await sAdminLogSystem.CurrentRoundLogs();
            return messages.Count >= amount;
        });
    }

    [Test]
    public async Task AddPlayerSessionLog()
    {
        var sPlayers = Server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();
        Guid playerGuid = default;

        await Server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.First();
            playerGuid = player.UserId;

            Assert.DoesNotThrow(() =>
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{player:Player} test log.");
            });
        });

        await PoolManager.WaitUntil(Server, async () =>
        {
            var logs = await sAdminLogSystem.CurrentRoundLogs();
            if (logs.Count == 0)
            {
                return false;
            }

            Assert.That(logs.First().Players, Does.Contain(playerGuid));
            return true;
        });
    }

    [Test]
    public async Task DuplicatePlayerDoesNotThrowTest()
    {
        var sPlayers = Server.ResolveDependency<IPlayerManager>();
        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await Server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player} {player} test log: {guid}");
        });

        await PoolManager.WaitUntil(Server, async () =>
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
    }

    [Test]
    public async Task DuplicatePlayerIdDoesNotThrowTest()
    {
        var sPlayers = Server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();

        var guid = Guid.NewGuid();

        await Server.WaitPost(() =>
        {
            var player = sPlayers.Sessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player:first} {player:second} test log: {guid}");
        });

        await PoolManager.WaitUntil(Server, async () =>
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
    }
}

public sealed class PreRoundAddTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings
    {
        Dirty = true,
        InLobby = true,
        AdminLogsEnabled = true
    };

    [Test]
    public async Task PreRoundAddAndGetSingle()
    {
        var sDatabase = Server.ResolveDependency<IServerDbManager>();
        var sSystems = Server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = Server.ResolveDependency<IAdminLogManager>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        await Server.WaitPost(() =>
        {
            sAdminLogSystem.Add(LogType.Unknown, $"test log: {guid}");
        });

        await Server.WaitPost(() =>
        {
            sGamerTicker.StartRound(true);
        });

        SharedAdminLog log = default;

        await PoolManager.WaitUntil(Server, async () =>
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
    }

}
