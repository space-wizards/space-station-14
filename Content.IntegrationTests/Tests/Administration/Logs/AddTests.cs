using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class AddTests : ContentIntegrationTest
{
    [Test]
    public async Task AddAndGetSingleLog()
    {
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
            Pool = true
        });
        await server.WaitIdleAsync();

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {guid}");
        });

        await WaitUntil(server, async () =>
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
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
            Pool = true
        });
        await server.WaitIdleAsync();

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}");
        });

        SharedAdminLog log = default;

        await WaitUntil(server, async () =>
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
    }

    [Test]
    [TestCase(500)]
    public async Task BulkAddLogs(int amount)
    {
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
            Pool = true
        });
        await server.WaitIdleAsync();

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            for (var i = 0; i < amount; i++)
            {
                sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
            }
        });

        await WaitUntil(server, async () =>
        {
            var messages = await sAdminLogSystem.CurrentRoundLogs();
            return messages.Count >= amount;
        });
    }

    [Test]
    public async Task AddPlayerSessionLog()
    {
        var (client, server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
            Pool = true
        });

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
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

        await WaitUntil(server, async () =>
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
    public async Task PreRoundAddAndGetSingle()
    {
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0",
                [CCVars.GameLobbyEnabled.Name] = "true"
            },
        });
        await server.WaitIdleAsync();

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGamerTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}");
        });

        await server.WaitPost(() =>
        {
            sGamerTicker.StartRound(true);
        });

        SharedAdminLog log = default;

        await WaitUntil(server, async () =>
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
    }

    [Test]
    public async Task DuplicatePlayerDoesNotThrowTest()
    {
        var (client, server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
        });

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.ServerSessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player} {player} test log: {guid}");
        });

        await WaitUntil(server, async () =>
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

        Assert.Pass();
    }

    [Test]
    public async Task DuplicatePlayerIdDoesNotThrowTest()
    {
        var (client, server) = await StartConnectedServerClientPair(serverOptions: new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            },
        });

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sPlayers = server.ResolveDependency<IPlayerManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var player = sPlayers.ServerSessions.Single();

            sAdminLogSystem.Add(LogType.Unknown, $"{player:first} {player:second} test log: {guid}");
        });

        await WaitUntil(server, async () =>
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

        Assert.Pass();
    }
}
