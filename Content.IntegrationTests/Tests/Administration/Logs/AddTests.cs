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
public class AddTests : ContentIntegrationTest
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

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity} test log: {guid}");
        });

        LogRecord log = null;

        await WaitUntil(server, async () =>
        {
            var logs = sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            await foreach (var found in logs)
            {
                log = found;
                return true;
            }

            return false;
        });

        var filter = new LogFilter
        {
            Round = log.RoundId,
            Search = log.Message,
            Types = new List<LogType> {log.Type},
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
    [TestCase(500, false)]
    [TestCase(500, true)]
    public async Task BulkAddLogs(int amount, bool parallel)
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

            if (parallel)
            {
                Parallel.For(0, amount, _ =>
                {
                    sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
                });
            }
            else
            {
                for (var i = 0; i < amount; i++)
                {
                    sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log.");
                }
            }
        });

        await WaitUntil(server, async () =>
        {
            var messages = sAdminLogSystem.CurrentRoundLogs();
            var count = 0;

            await foreach (var _ in messages)
            {
                count++;
            }

            return count >= amount;
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
            var logs = sAdminLogSystem.CurrentRoundLogs();

            await foreach (var log in logs)
            {
                Assert.That(log.Players, Does.Contain(playerGuid));
                return true;
            }

            return false;
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

        LogRecord log = null;

        await WaitUntil(server, async () =>
        {
            var logs = sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = guid.ToString()
            });

            await foreach (var found in logs)
            {
                log = found;
                return true;
            }

            return false;
        });

        var filter = new LogFilter
        {
            Round = log.RoundId,
            Search = log.Message,
            Types = new List<LogType> {log.Type},
        };

        await foreach (var json in sDatabase.GetAdminLogsJson(filter))
        {
            var root = json.RootElement;

            Assert.That(root.TryGetProperty("entity", out _), Is.True);
            Assert.That(root.TryGetProperty("guid", out _), Is.True);

            json.Dispose();
        }
    }
}
