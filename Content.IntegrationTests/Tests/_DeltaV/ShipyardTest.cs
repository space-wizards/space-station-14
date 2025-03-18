using System.Linq;
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
    private static readonly string[] NoShuttleConsole =
    {
        "BargainBin", // This is supposed to be a ton of trash I believe, so sometimes it'll spawn without a console :)
    };

    private static readonly string[] NoShuttleDock =
    {
        "Strugglebug", // No dock because fuck you
    };

    private static readonly string[] IgnoreArbitrage =
    {
        "BargainBin",
    };

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
                    foreach (var path in vessel.Path) // Imp - Fix for shuttles with multiple grids (I hope the actual loader supports this too lol)
                    {
                        if (IgnoreArbitrage.Contains(vessel.ID))
                            continue;

                        var shuttle = shipyard.TryCreateShuttle(path);
                        Assert.That(shuttle, Is.Not.Null, $"Failed to spawn shuttle {vessel.ID}({path})!");
                        var value = pricing.AppraiseGrid(shuttle.Value);
                        Assert.That(value, Is.AtMost(vessel.Price), $"Found arbitrage on shuttle {vessel.ID}({path})! Price is {vessel.Price} but value is {value}!");
                        entities.DeleteEntity(shuttle);
                    }
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
                    foreach (var path in vessel.Path) // Imp - Fix for shuttles with multiple grids (I hope the actual loader supports this too lol)
                    {
                        var shuttle = shipyard.TryCreateShuttle(new ResPath(path.ToString()));
                        Assert.That(shuttle, Is.Not.Null, $"Failed to spawn shuttle {vessel.ID}({path})!");
                        var console = FindComponent<ShuttleConsoleComponent>(entities, shuttle.Value) || NoShuttleConsole.Contains(vessel.ID); // Imp - Add skipping console test
                        Assert.That(console, Is.True, $"Shuttle {vessel.ID}({path}) had no shuttle console!");
                        var dock = FindComponent<DockingComponent>(entities, shuttle.Value) || NoShuttleDock.Contains(vessel.ID); // Imp - Add skipping dock test
                        Assert.That(dock, Is.True, $"Shuttle {vessel.ID}({path})  had no shuttle dock!");
                        entities.DeleteEntity(shuttle);
                    }
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
