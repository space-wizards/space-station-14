using System.Threading.Tasks;
using Content.Server.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using System.Diagnostics.CodeAnalysis;

namespace Content.IntegrationTests.Tests.Buckle
{
    [TestFixture]
    [TestOf(typeof(BuckleComponent))]
    [TestOf(typeof(StrapComponent))]
    public sealed class BuckleTest
    {
        private const string BuckleDummyId = "BuckleDummy";
        private const string StrapDummyId = "StrapDummy";
        private const string ItemDummyId = "ItemDummy";

        private static readonly string Prototypes = $@"
- type: entity
  name: {BuckleDummyId}
  id: {BuckleDummyId}
  components:
  - type: Buckle
  - type: Hands
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

        [Test]
        [SuppressMessage("Assertion", "NUnit2045:Use Assert.Multiple", Justification = "The assertions in the buckle test are interdependent.")]
        public async Task BuckleUnbuckleCooldownRangeTest()
        {
            await using var pairTracker =
                await PoolManager.GetServerClient(new PoolSettings { ExtraPrototypes = Prototypes });
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var actionBlocker = entityManager.EntitySysManager.GetEntitySystem<ActionBlockerSystem>();
            var buckleSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBuckleSystem>();
            var standingState = entityManager.EntitySysManager.GetEntitySystem<StandingStateSystem>();
            var xformSystem = entityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Default state, unbuckled
                Assert.That(entityManager.TryGetComponent(human, out buckle), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(buckle, Is.Not.Null);
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human), Is.True);
                    Assert.That(actionBlocker.CanChangeDirection(human), Is.True);
                    Assert.That(standingState.Down(human), Is.True);
                    Assert.That(standingState.Stand(human), Is.True);
                });

                // Default state, no buckled entities, strap
                Assert.That(entityManager.TryGetComponent(chair, out strap), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(strap, Is.Not.Null);
                    Assert.That(strap.BuckledEntities, Is.Empty);
                    Assert.That(strap.OccupiedSize, Is.Zero);
                });

                // Side effects of buckling
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled, Is.True);
                });

                Assert.That(actionBlocker.CanMove(human), Is.False);
                Assert.That(actionBlocker.CanChangeDirection(human), Is.False);
                Assert.That(standingState.Down(human), Is.False);
                Assert.That((xformSystem.GetWorldPosition(human) - xformSystem.GetWorldPosition(chair)).LengthSquared, Is.LessThanOrEqualTo(0));

                // Side effects of buckling for the strap
                Assert.Multiple(() =>
                {
                    Assert.That(strap.BuckledEntities, Does.Contain(human));
                    Assert.That(strap.OccupiedSize, Is.EqualTo(buckle.Size));
                    Assert.Positive(strap.OccupiedSize);
                });

                // Trying to buckle while already buckled fails
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle), Is.False);

                // Trying to unbuckle too quickly fails
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled, Is.True);

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human), Is.True);
                    Assert.That(actionBlocker.CanChangeDirection(human), Is.True);
                    Assert.That(standingState.Down(human), Is.True);
                    Assert.That(standingState.Stand(human), Is.True);
                });

                // Unbuckle, strap
                Assert.Multiple(() =>
                {
                    Assert.That(strap.BuckledEntities, Is.Empty);
                    Assert.That(strap.OccupiedSize, Is.Zero);
                });

                // Re-buckling has no cooldown
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);
                Assert.That(buckle.Buckled, Is.True);

                // On cooldown
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled, Is.True);

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.True);
                Assert.That(buckle.Buckled, Is.False);

                // Move away from the chair
                var oldWorldPosition = xformSystem.GetWorldPosition(human);
                xformSystem.SetWorldPosition(human, oldWorldPosition + (1000, 1000));

                // Out of range
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.False);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);

                // Move near the chair
                var chairWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, chairWorldPosition + (0.5f, 0));

                // In range
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);
                Assert.That(buckle.Buckled, Is.True);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled, Is.True);

                // Force unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle), Is.True);
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human), Is.True);
                    Assert.That(actionBlocker.CanChangeDirection(human), Is.True);
                    Assert.That(standingState.Down(human), Is.True);
                    Assert.That(standingState.Stand(human), Is.True);
                });

                // Re-buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);

                // Move away from the chair
                oldWorldPosition = xformSystem.GetWorldPosition(human);
                xformSystem.SetWorldPosition(human, oldWorldPosition + (1, 0));
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // No longer buckled
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(strap.BuckledEntities, Is.Empty);
                });
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        [SuppressMessage("Assertion", "NUnit2045:Use Assert.Multiple", Justification = "The assertions in the buckle test are interdependent.")]
        public async Task BuckledDyingDropItemsTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
            {
                NoClient = true, ExtraPrototypes = Prototypes
            });
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;

            EntityUid human = default;
            BuckleComponent buckle = null;
            HandsComponent hands = null;
            BodyComponent body = null;

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var handsSys = entityManager.EntitySysManager.GetEntitySystem<SharedHandsSystem>();
            var buckleSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBuckleSystem>();

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                var chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.That(entityManager.TryGetComponent(human, out buckle), Is.True);
                Assert.That(entityManager.HasComponent<StrapComponent>(chair), Is.True);
                Assert.That(entityManager.TryGetComponent(human, out hands), Is.True);
                Assert.That(entityManager.TryGetComponent(human, out body), Is.True);

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled, Is.True);

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = entityManager.SpawnEntity(ItemDummyId, coordinates);

                    Assert.That(handsSys.TryPickupAnyHand(human, akms), Is.True);
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled, Is.True);

                // With items in all hands
                foreach (var hand in hands.Hands.Values)
                {
                    Assert.That(hand.HeldEntity, Is.Not.Null);
                }

                var bodySystem = entityManager.System<BodySystem>();
                var legs = bodySystem.GetBodyChildrenOfType(human, BodyPartType.Leg, body);

                // Break our guy's kneecaps
                foreach (var leg in legs)
                {
                    bodySystem.DropPart(leg.Id, leg.Component);
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled, Is.True);

                // Now with no item in any hand
                foreach (var hand in hands.Hands.Values)
                {
                    Assert.That(hand.HeldEntity, Is.Null);
                }

                buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        [SuppressMessage("Assertion", "NUnit2045:Use Assert.Multiple", Justification = "The assertions in the buckle test are interdependent.")]
        public async Task ForceUnbuckleBuckleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
            {
                NoClient = true, ExtraPrototypes = Prototypes
            });
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var buckleSystem = entityManager.System<SharedBuckleSystem>();
            var xformSystem = entityManager.System<SharedTransformSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.That(entityManager.TryGetComponent(human, out buckle), Is.True);
                Assert.That(entityManager.HasComponent<StrapComponent>(chair), Is.True);

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled, Is.True);

                // Move the buckled entity away
                var oldWorldPosition = xformSystem.GetWorldPosition(human);
                xformSystem.SetWorldPosition(human, oldWorldPosition + (100, 0));
            });

            await PoolManager.WaitUntil(server, () => !buckle.Buckled, 10);

            Assert.That(buckle.Buckled, Is.False);

            await server.WaitAssertion(() =>
            {
                // Move the now unbuckled entity back onto the chair
                var oldWorldPosition = xformSystem.GetWorldPosition(human);
                xformSystem.SetWorldPosition(human, oldWorldPosition - (100, 0));

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.True);
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled, Is.True);
            });

            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.BuckledTo, Is.Not.Null);
                Assert.That(buckle.Buckled, Is.True);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
