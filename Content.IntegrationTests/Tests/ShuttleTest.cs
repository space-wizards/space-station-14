#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests;

public sealed class ShuttleTest : GameTest
{
    [SidedDependency(Side.Server)] private SharedPhysicsSystem _sPhysics = default!;

    [Test]
    [Description($"Tests that grids have the {nameof(ShuttleComponent)} and move when pushed.")]
    public async Task Test()
    {
        await Pair.CreateTestMap();

        Assume.That(TestMap, Is.Not.Null);

        await Server.WaitAssertion(() =>
        {
            var mapId = TestMap.MapId;
            var grid = TestMap.Grid;
            PhysicsComponent? gridPhys = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(grid, Has.Comp<ShuttleComponent>(Server));
                Assert.That(SEntMan.TryGetComponent(grid, out gridPhys));
            }
            using (Assert.EnterMultipleScope())
            {
                Assert.That(gridPhys!.BodyType, Is.EqualTo(BodyType.Dynamic));
                Assert.That(SComp<TransformComponent>(grid).LocalPosition, Is.EqualTo(Vector2.Zero));
            }
            _sPhysics.ApplyLinearImpulse(grid, Vector2.One, body: gridPhys);

            Server.RunTicks(1);
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(SComp<TransformComponent>(TestMap.Grid).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
        });
    }
}
