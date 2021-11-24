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
public class FilterTests : ContentIntegrationTest
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
        var guids = new[] {Guid.NewGuid(), Guid.NewGuid()};

        for (var i = 0; i < 2; i++)
        {
            await server.WaitPost(() =>
            {
                var coordinates = GetMainEntityCoordinates(sMaps);
                var entity = sEntities.SpawnEntity(null, coordinates);

                sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {guids[i]}");
            });

            await server.WaitRunTicks(60);
        }

        await WaitUntil(server, async () =>
        {
            var commonGuidStr = commonGuid.ToString();

            string firstGuidStr;
            string secondGuidStr;

            switch (order)
            {
                case DateOrder.Ascending:
                    // Oldest first
                    firstGuidStr = guids[0].ToString();
                    secondGuidStr = guids[1].ToString();
                    break;
                case DateOrder.Descending:
                    // Newest first
                    firstGuidStr = guids[1].ToString();
                    secondGuidStr = guids[0].ToString();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }

            var firstFound = false;
            var secondFound = false;

            var both = sAdminLogSystem.CurrentRoundLogs(new LogFilter
            {
                Search = commonGuidStr,
                DateOrder = order
            });

            await foreach (var log in both)
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
