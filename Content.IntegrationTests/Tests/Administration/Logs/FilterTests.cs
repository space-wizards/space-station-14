using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Administration.Logs;

[TestFixture]
[TestOf(typeof(AdminLogSystem))]
public sealed class FilterTests
{
    [Test]
    [TestCase(DateOrder.Ascending)]
    [TestCase(DateOrder.Descending)]
    public async Task Date(DateOrder order)
    {
        await using var pair = await PoolManager.GetServerClient(AddTests.LogTestSettings);
        var server = pair.Server;

        var sEntities = server.ResolveDependency<IEntityManager>();

        var sAdminLogSystem = server.ResolveDependency<IAdminLogManager>();

        var commonGuid = Guid.NewGuid();
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();
        var testMap = await pair.CreateTestMap();
        var coordinates = testMap.GridCoords;

        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {firstGuid}");
        });

        await Task.Delay(2000);

        await server.WaitPost(() =>
        {
            var entity = sEntities.SpawnEntity(null, coordinates);

            sAdminLogSystem.Add(LogType.Unknown, $"{entity:Entity} test log: {commonGuid} {secondGuid}");
        });

        await PoolManager.WaitUntil(server, async () =>
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
        await pair.CleanReturnAsync();
    }
}
