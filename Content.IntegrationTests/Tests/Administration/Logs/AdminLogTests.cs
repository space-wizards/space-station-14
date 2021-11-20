using System;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public class AdminLogsTests : ContentIntegrationTest
{
    [Test]
    public async Task AddAndGetSingleLogTest()
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
            var messages = sAdminLogSystem.AllMessages(new LogFilter
            {
                Round = sGameTicker.RoundId,
                Search = guid.ToString()
            });

            await foreach (var _ in messages)
            {
                return true;
            }

            return false;
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
            var messages = sAdminLogSystem.AllMessages(new LogFilter
            {
                Round = sGameTicker.RoundId
            });

            var count = 0;

            await foreach (var _ in messages)
            {
                count++;
            }

            return count >= amount;
        });
    }
}
