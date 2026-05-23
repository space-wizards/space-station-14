#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Buckle;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Content.IntegrationTests.Fixtures.Attributes;

namespace Content.IntegrationTests.Tests.Buckle;

[TestFixture]
[TestOf(typeof(BuckleComponent))]
[TestOf(typeof(StrapComponent))]
public sealed partial class BuckleTest : GameTest
{
    private const string BuckleDummyId = "BuckleDummy";
    private const string StrapDummyId = "StrapDummy";
    private const string ItemDummyId = "ItemDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {BuckleDummyId}
  id: {BuckleDummyId}
  components:
  - type: Buckle
  - type: Hands
  - type: ComplexInteraction
  - type: InputMover
  - type: Physics
    bodyType: KinematicController
  - type: Body
    prototype: Human
  - type: StandingState

- type: entity
  name: {StrapDummyId}
  id: {StrapDummyId}
  components:
  - type: Strap

- type: entity
  name: {ItemDummyId}
  id: {ItemDummyId}
  components:
  - type: Item
";

    [SidedDependency(Side.Server)] private ActionBlockerSystem _sActionBlocker = null!;
    [SidedDependency(Side.Server)] private SharedBuckleSystem _sBuckle = null!;
    [SidedDependency(Side.Server)] private StandingStateSystem _sStatingState = null!;
    [SidedDependency(Side.Server)] private SharedTransformSystem _sTransform = null!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _sHands = null!;

    [Test]
    public async Task BuckleUnbuckleCooldownRangeTest()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid human = default;
        EntityUid chair = default;
        BuckleComponent buckle = null!;
        StrapComponent strap = null!;

        await Server.WaitAssertion(() =>
        {
            human = SSpawnAtPosition(BuckleDummyId, coordinates);
            chair = SSpawnAtPosition(StrapDummyId, coordinates);

            // Default state, unbuckled
            buckle = SComp<BuckleComponent>(human);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle, Is.Not.Null);
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(_sActionBlocker.CanMove(human));
                Assert.That(_sActionBlocker.CanChangeDirection(human));
                Assert.That(_sStatingState.Down(human));
                Assert.That(_sStatingState.Stand(human));
            }

            // Default state, no buckled entities, strap
            strap = SComp<StrapComponent>(chair);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(strap, Is.Not.Null);
                Assert.That(strap.BuckledEntities, Is.Empty);
            }

            // Side effects of buckling
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckle));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled);

                Assert.That(_sActionBlocker.CanMove(human), Is.False);
                Assert.That(_sActionBlocker.CanChangeDirection(human));
                Assert.That(_sStatingState.Down(human), Is.False);
                Assert.That(
                    (_sTransform.GetWorldPosition(human) - _sTransform.GetWorldPosition(chair)).LengthSquared(),
                    Is.LessThanOrEqualTo(0)
                );

                // Side effects of buckling for the strap
                Assert.That(strap.BuckledEntities, Does.Contain(human));
            }

#pragma warning disable NUnit2045 // Interdependent asserts.
            // Trying to buckle while already buckled fails
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckle), Is.False);

            // Trying to unbuckle too quickly fails
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
            Assert.That(buckle.Buckled);
            Assert.That(_sBuckle.TryUnbuckle(human, human), Is.False);
            Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
        });

        // Wait enough ticks for the unbuckling cooldown to run out
        await Server.WaitRunTicks(60);

        await Server.WaitAssertion(() =>
        {
#pragma warning disable NUnit2045 // Interdependent asserts.
            Assert.That(buckle.Buckled);
            // Still buckled
#pragma warning restore NUnit2045

            // Unbuckle
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(_sActionBlocker.CanMove(human));
                Assert.That(_sActionBlocker.CanChangeDirection(human));
                Assert.That(_sStatingState.Down(human));

                // Unbuckle, strap
                Assert.That(strap.BuckledEntities, Is.Empty);
            }

#pragma warning disable NUnit2045 // Interdependent asserts.
            // Re-buckling has no cooldown
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));
            Assert.That(buckle.Buckled);

            // On cooldown
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
            Assert.That(buckle.Buckled);
            Assert.That(_sBuckle.TryUnbuckle(human, human), Is.False);
            Assert.That(buckle.Buckled);
            Assert.That(_sBuckle.TryUnbuckle(human, human), Is.False);
            Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
        });

        // Wait enough ticks for the unbuckling cooldown to run out
        await Server.WaitRunTicks(60);

        await Server.WaitAssertion(() =>
        {
#pragma warning disable NUnit2045 // Interdependent asserts.
            // Still buckled
            Assert.That(buckle.Buckled);

            // Unbuckle
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle));
            Assert.That(buckle.Buckled, Is.False);
#pragma warning restore NUnit2045

            // Move away from the chair
            var oldWorldPosition = _sTransform.GetWorldPosition(chair);
            _sTransform.SetWorldPosition(human, oldWorldPosition + new Vector2(1000, 1000));

            // Out of range
#pragma warning disable NUnit2045 // Interdependent asserts.
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle), Is.False);
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
#pragma warning restore NUnit2045

            // Move near the chair
            oldWorldPosition = _sTransform.GetWorldPosition(chair);
            _sTransform.SetWorldPosition(human, oldWorldPosition + new Vector2(0.5f, 0));

            // In range
#pragma warning disable NUnit2045 // Interdependent asserts.
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));
            Assert.That(buckle.Buckled);
            Assert.That(_sBuckle.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
            Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045

            // Force unbuckle
            _sBuckle.Unbuckle(human, human);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(_sActionBlocker.CanMove(human));
                Assert.That(_sActionBlocker.CanChangeDirection(human));
                Assert.That(_sStatingState.Down(human));
            }

            // Re-buckle
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));

            // Move away from the chair
            oldWorldPosition = _sTransform.GetWorldPosition(chair);
            _sTransform.SetWorldPosition(human, oldWorldPosition + new Vector2(1, 0));
        });

        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            // No longer buckled
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(strap.BuckledEntities, Is.Empty);
            }
        });
    }

    [Test]
    public async Task BuckledDyingDropItemsTest()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid human = default;
        BuckleComponent? buckle = null;
        HandsComponent? hands = null;

        await Server.WaitIdleAsync();

        await Server.WaitAssertion(() =>
        {
            human = SSpawnAtPosition(BuckleDummyId, coordinates);
            var chair = SSpawnAtPosition(StrapDummyId, coordinates);

            // Component sanity check
            using (Assert.EnterMultipleScope())
            {
                Assert.That(STryComp(human, out buckle));
                Assert.That(STryComp<StrapComponent>(chair, out _));
                Assert.That(STryComp(human, out hands));
            }

            // Buckle
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle!.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled);
            }

            // Put an item into every hand
            for (var i = 0; i < hands!.Count; i++)
            {
                var akms = SSpawnAtPosition(ItemDummyId, coordinates);

                Assert.That(_sHands.TryPickupAnyHand(human, akms));
            }
        });

        await Server.WaitRunTicks(10);

        await Server.WaitAssertion(() =>
        {
            // Still buckled
            Assert.That(buckle!.Buckled);

            // Still with items in hand
            foreach (var hand in hands!.Hands.Keys)
            {
                Assert.That(_sHands.GetHeldItem((human, hands), hand), Is.Not.Null);
            }

            _sBuckle.Unbuckle(human, human);
            Assert.That(buckle.Buckled, Is.False);
        });
    }

    [Test]
    public async Task ForceUnbuckleBuckleTest()
    {
        await Pair.CreateTestMap();
        var coordinates = TestMap!.GridCoords;

        EntityUid human = default;
        EntityUid chair = default;
        BuckleComponent? buckle = null!;

        await Server.WaitAssertion(() =>
        {
            human = SSpawnAtPosition(BuckleDummyId, coordinates);
            chair = SSpawnAtPosition(StrapDummyId, coordinates);

            // Component sanity check
            using (Assert.EnterMultipleScope())
            {
                Assert.That(STryComp(human, out buckle));
                Assert.That(STryComp<StrapComponent>(chair, out _));
            }

            // Buckle
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle!.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled);
            }

            // Move the buckled entity away
            var oldWorldPosition = _sTransform.GetWorldPosition(chair);
            _sTransform.SetWorldPosition(human, oldWorldPosition + new Vector2(100, 0));
        });

        await PoolManager.WaitUntil(Server, () => !buckle.Buckled, 10);

        Assert.That(buckle.Buckled, Is.False);

        await Server.WaitAssertion(() =>
        {
            // Move the now unbuckled entity back onto the chair
            var oldWorldPosition = _sTransform.GetWorldPosition(chair);
            _sTransform.SetWorldPosition(human, oldWorldPosition);

            // Buckle
            Assert.That(_sBuckle.TryBuckle(human, human, chair, buckleComp: buckle));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled);
            }
        });

        await Server.WaitRunTicks(60);

        await Server.WaitAssertion(() =>
        {
            // Still buckled
            using (Assert.EnterMultipleScope())
            {
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled);
            }
        });
    }
}
