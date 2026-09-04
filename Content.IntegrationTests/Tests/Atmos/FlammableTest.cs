using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
public sealed class FlammableTest : GameTest
{
    [Test]
    public async Task FireSpreadsOnlyWithinConfiguredRadius()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var flammableSystem = entMan.System<FlammableSystem>();
        var map = await Pair.CreateTestMap();

        EntityUid source = default;
        EntityUid nearby = default;
        EntityUid distant = default;

        await server.WaitPost(() =>
        {
            source = entMan.SpawnEntity("Carpet", map.GridCoords);
            nearby = entMan.SpawnEntity("Carpet", map.GridCoords.Offset(new Vector2(1f, 0f)));
            distant = entMan.SpawnEntity("Carpet", map.GridCoords.Offset(new Vector2(2f, 0f)));

            var sourceFlammable = entMan.GetComponent<FlammableComponent>(source);
            sourceFlammable.FireStacks = 4f;
            sourceFlammable.OnFire = true;

            flammableSystem.SpreadFire((source, sourceFlammable));
        });

        var nearbyFlammable = entMan.GetComponent<FlammableComponent>(nearby);
        var distantFlammable = entMan.GetComponent<FlammableComponent>(distant);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nearbyFlammable.OnFire, Is.True);
            Assert.That(nearbyFlammable.FireStacks, Is.EqualTo(2f));
            Assert.That(distantFlammable.OnFire, Is.False);
            Assert.That(distantFlammable.FireStacks, Is.Zero);
        }
    }
}
