using Content.Server.Cargo.Systems;
using Content.Server._DV.Shipyard;
using Content.Server.Shuttles.Components;
using Content.Shared._DV.Shipyard.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.DV;

[TestFixture]
[TestOf(typeof(ShipyardSystem))]
public sealed class ShipyardTest
{
    [Test]
    public async Task NoShipyardArbitrage()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entities = server.ResolveDependency<IEntityManager>();
        var proto = server.ResolveDependency<IPrototypeManager>();
        var shipyard = entities.System<ShipyardSystem>();
        var pricing = entities.System<PricingSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in proto.EnumeratePrototypes<VesselPrototype>())
                {
                    var shuttle = shipyard.TryCreateShuttle(new ResPath(vessel.Path.ToString()));
                    Assert.That(shuttle, Is.Not.Null, $"Failed to spawn shuttle {vessel.ID}!");
                    var value = pricing.AppraiseGrid(shuttle.Value);
                    Assert.That(value, Is.AtMost(vessel.Price), $"Found arbitrage on shuttle {vessel.ID}! Price is {vessel.Price} but value is {value}!");
                    entities.DeleteEntity(shuttle);
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AllShuttlesValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entities = server.ResolveDependency<IEntityManager>();
        var proto = server.ResolveDependency<IPrototypeManager>();
        var shipyard = entities.System<ShipyardSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var vessel in proto.EnumeratePrototypes<VesselPrototype>())
                {
                    var shuttle = shipyard.TryCreateShuttle(new ResPath(vessel.Path.ToString()));
                    Assert.That(shuttle, Is.Not.Null, $"Failed to spawn shuttle {vessel.ID}!");
                    var console = FindComponent<ShuttleConsoleComponent>(entities, shuttle.Value);
                    Assert.That(console, Is.True, $"Shuttle {vessel.ID} had no shuttle console!");
                    var dock = FindComponent<DockingComponent>(entities, shuttle.Value);
                    Assert.That(dock, Is.True, $"Shuttle {vessel.ID} had no shuttle dock!");
                    entities.DeleteEntity(shuttle);
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    private bool FindComponent<T>(IEntityManager entities, EntityUid shuttle) where T: Component
    {
        var query = entities.EntityQueryEnumerator<T, TransformComponent>();
        while (query.MoveNext(out _, out var xform))
        {
            if (xform.ParentUid != shuttle)
                continue;

            return true;
        }

        return false;
    }
}
