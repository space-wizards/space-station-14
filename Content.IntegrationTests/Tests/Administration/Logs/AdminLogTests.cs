using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public class AdminLogsTests : ContentIntegrationTest
{
    [Test]
    public async Task AddAndGetSingleLog()
    {
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            }
        });
        await server.WaitIdleAsync();

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGameTicker = sSystems.GetEntitySystem<GameTicker>();

        var guid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {guid}");
        });

        await WaitUntil(server, async () =>
        {
            var logs = sAdminLogSystem.AllJson(new LogFilter
            {
                Round = sGameTicker.RoundId,
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
            }
        });
        await server.WaitIdleAsync();

        var sDatabase = server.ResolveDependency<IServerDbManager>();
        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGameTicker = sSystems.GetEntitySystem<GameTicker>();

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
            var logs = sAdminLogSystem.All(new LogFilter
            {
                Round = sGameTicker.RoundId,
                Search = guid.ToString()
            });

            await foreach (var found in logs)
            {
                log = found;
                return true;
            }

            return false;
        });

        await server.WaitPost(() =>
        {
            Task.Run(async () =>
            {
                var filter = new LogFilter
                {
                    Round = log.RoundId,
                    Search = log.Message,
                    Types = new List<LogType> {log.Type}
                };

                await foreach (var json in sDatabase.GetAdminLogsJson(filter))
                {
                    var root = json.RootElement;

                    Assert.That(root.TryGetProperty("entity", out _), Is.True);
                    Assert.That(root.TryGetProperty("guid", out _), Is.True);

                    json.Dispose();
                }
            }).Wait();
        });
    }

    [Test]
    [TestCase(500, false)]
    [TestCase(500, true)]
    public async Task AddLogs(int amount, bool parallel)
    {
        var server = StartServer(new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            }
        });
        await server.WaitIdleAsync();

        var sEntities = server.ResolveDependency<IEntityManager>();
        var sMaps = server.ResolveDependency<IMapManager>();
        var sSystems = server.ResolveDependency<IEntitySystemManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGameTicker = sSystems.GetEntitySystem<GameTicker>();

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
            var messages = sAdminLogSystem.All(new LogFilter
            {
                Round = sGameTicker.RoundId
            });

            var count = 0;

            await foreach (var _ in messages)
            {
                count++;
            }

            return count >= amount;
        }, 20000);
    }

    [Test]
    public async Task QueryLogs()
    {
        var serverOptions = new ServerContentIntegrationOption
        {
            CVarOverrides =
            {
                [CCVars.AdminLogsQueueSendDelay.Name] = "0"
            }
        };
        var (client, server) = await StartConnectedServerClientPair(serverOptions: serverOptions);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var sSystems = server.ResolveDependency<IEntitySystemManager>();
        var sPlayers = server.ResolveDependency<IPlayerManager>();

        var sAdminLogSystem = sSystems.GetEntitySystem<AdminLogSystem>();
        var sGameTicker = sSystems.GetEntitySystem<GameTicker>();

        var date = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        IPlayerSession player = default;

        await server.WaitPost(() =>
        {
            player = sPlayers.GetAllPlayers().First();

            sAdminLogSystem.Add(LogType.Unknown, $"{player.AttachedEntity:Entity} test log: {guid}");
        });

        var filter = new LogFilter
        {
            Round = sGameTicker.RoundId,
            Search = guid.ToString(),
            Types = new List<LogType> {LogType.Unknown},
            After = date,
            AnyPlayers = new[] {player.UserId.UserId}
        };

        await WaitUntil(server, async () =>
        {
            await foreach (var _ in sAdminLogSystem.All(filter))
            {
                return true;
            }

            return false;
        });
    }
}
