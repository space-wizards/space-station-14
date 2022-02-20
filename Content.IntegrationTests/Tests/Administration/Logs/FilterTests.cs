using System;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class FilterTests : ContentIntegrationTest
{
    [Test]
    [TestCase(DateOrder.Ascending)]
    [TestCase(DateOrder.Descending)]
    public async Task Date(DateOrder order)
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

        var commonGuid = Guid.NewGuid();
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {firstGuid}");
        });

        await Task.Delay(2000);

        await server.WaitPost(() =>
        {
            var coordinates = GetMainEntityCoordinates(sMaps);
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {secondGuid}");
        });

        await WaitUntil(server, async () =>
        {
            var commonGuidStr = commonGuid.ToString();

            string firstGuidStr;
            string secondGuidStr;

            switch (order)
            {
                case DateOrder.Ascending:
                    // Oldest first
                    firstGuidStr = firstGuid.ToString();
                    secondGuidStr = secondGuid.ToString();
                    break;
                case DateOrder.Descending:
                    // Newest first
                    firstGuidStr = secondGuid.ToString();
                    secondGuidStr = firstGuid.ToString();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }

            var firstFound = false;
            var secondFound = false;

            var both = await sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = commonGuidStr,
                DateOrder = order
            });

            foreach (var log in both)
            {
                if (!log.Message.Contains(commonGuidStr))
                {
                    continue;
                }

                if (!firstFound)
                {
                    Assert.That(log.Message, Does.Contain(firstGuidStr));
                    firstFound = true;
                    continue;
                }

                Assert.That(log.Message, Does.Contain(secondGuidStr));
                secondFound = true;
                break;
            }

            return firstFound && secondFound;
        });
    }
}
