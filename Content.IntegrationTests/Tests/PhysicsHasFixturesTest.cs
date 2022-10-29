using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Threading.Tasks;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class PhysicsFixtureTest
{
    [Test]
    public async Task PhysicsHasFixturesTest()
    {
        // Ensures that every prototype with physics has at least one fixture. Required for entity lookups to work properly.

        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true, Destructive = true });
        var server = pairTracker.Pair.Server;

        IEntityManager ent = null;

        await server.WaitPost(() =>
        {
            ent = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
            var protoIds = prototypeMan
                .EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Select(p => p.ID);
            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var coord = new EntityCoordinates(grid.GridEntityId, 0, 0);
            foreach (var protoId in protoIds)
            {
                ent.SpawnEntity(protoId, coord);
            }
        });
        await server.WaitRunTicks(5);
        await server.WaitPost(() =>
        {
            var physics = ent.EntityQuery<PhysicsComponent>(true);
            var fixturesQuery = ent.GetEntityQuery<FixturesComponent>();
            Assert.Multiple(() =>
            {
                foreach (var body in physics)
                {
                    Assert.That(fixturesQuery.GetComponent(body.Owner).FixtureCount > 0,
                        $"{ent.ToPrettyString(body.Owner)} has a physics component without any fixtures!");
                }
            });
        });
        await pairTracker.CleanReturnAsync();
    }
}
