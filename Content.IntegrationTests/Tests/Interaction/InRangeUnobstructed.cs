#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Interaction;

[TestOf(typeof(SharedInteractionSystem))]
public sealed class InRangeUnobstructed : GameTest
{
    private static readonly EntProtoId HumanId = "MobHuman";

    private const float InteractionRange = SharedInteractionSystem.InteractionRange;
    private const float InteractionRangeDivided15 = InteractionRange / 1.5f;
    private static readonly Vector2 InteractionRangeDivided15X = new(InteractionRangeDivided15, 0f);
    private const float InteractionRangeDivided15Times3 = InteractionRangeDivided15 * 3;
    private const float HumanRadius = 0.35f;

    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = default!;
    [SidedDependency(Side.Server)] private SharedInteractionSystem _sInteractionSystem = default!;
    [SidedDependency(Side.Server)] private TransformSystem _sTransformSystem = default!;

    [Test]
    public async Task EntityEntityTest()
    {
        EntityUid origin = default;
        EntityUid other = default;
        MapCoordinates mapCoordinates = default;

        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);

        await Server.WaitAssertion(() =>
        {
            var coordinates = TestMap.GridCoords;

            origin = SSpawnAtPosition(HumanId, coordinates);
            other = SSpawnAtPosition(HumanId, coordinates);
            _sContainerSystem.EnsureContainer<Container>(other, "InRangeUnobstructedTestOtherContainer");
            mapCoordinates = _sTransformSystem.GetMapCoordinates(other);
        });

        await Server.WaitIdleAsync();

        var xform = SComp<TransformComponent>(origin);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                // Entity <-> Entity
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, other));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(other, origin));

                // Entity <-> MapCoordinates
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, mapCoordinates));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(mapCoordinates, origin));
            }

            // Move them slightly apart
            _sTransformSystem.SetLocalPosition(origin, xform.LocalPosition + InteractionRangeDivided15X, xform);

            Assert.Multiple(() =>
            {
                // Entity <-> Entity
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, other));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(other, origin));

                // Entity <-> MapCoordinates
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, mapCoordinates));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(mapCoordinates, origin));
            });

            // Move them out of range
            _sTransformSystem.SetLocalPosition(origin, xform.LocalPosition + new Vector2(InteractionRangeDivided15 + HumanRadius * 2f, 0f), xform);

            using (Assert.EnterMultipleScope())
            {
                // Entity <-> Entity
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, other), Is.False);
                Assert.That(_sInteractionSystem.InRangeUnobstructed(other, origin), Is.False);

                // Entity <-> MapCoordinates
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, mapCoordinates), Is.False);
                Assert.That(_sInteractionSystem.InRangeUnobstructed(mapCoordinates, origin), Is.False);

                // Checks with increased range

                // Entity <-> Entity
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, other, InteractionRangeDivided15Times3));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(other, origin, InteractionRangeDivided15Times3));

                // Entity <-> MapCoordinates
                Assert.That(_sInteractionSystem.InRangeUnobstructed(origin, mapCoordinates, InteractionRangeDivided15Times3));
                Assert.That(_sInteractionSystem.InRangeUnobstructed(mapCoordinates, origin, InteractionRangeDivided15Times3));
            }
        });
    }
}
