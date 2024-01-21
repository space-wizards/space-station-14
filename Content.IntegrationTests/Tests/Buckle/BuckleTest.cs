using System.Numerics;
using Content.Server.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

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

        [TestPrototypes]
        private const string Prototypes = $@"
- type: entity
  name: {BuckleDummyId}
  id: {BuckleDummyId}
  components:
  - type: Buckle
  - type: Hands
  - type: InputMover
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
        public async Task BuckleUnbuckleCooldownRangeTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var actionBlocker = entityManager.System<ActionBlockerSystem>();
            var buckleSystem = entityManager.System<SharedBuckleSystem>();
            var standingState = entityManager.System<StandingStateSystem>();
            var xformSystem = entityManager.System<SharedTransformSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Default state, unbuckled
                Assert.That(entityManager.TryGetComponent(human, out buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle, Is.Not.Null);
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));
                    Assert.That(standingState.Stand(human));
                });

                // Default state, no buckled entities, strap
                Assert.That(entityManager.TryGetComponent(chair, out strap));
                Assert.Multiple(() =>
                {
                    Assert.That(strap, Is.Not.Null);
                    Assert.That(strap.BuckledEntities, Is.Empty);
                    Assert.That(strap.OccupiedSize, Is.Zero);
                });

                // Side effects of buckling
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);

                    Assert.That(actionBlocker.CanMove(human), Is.False);
                    Assert.That(actionBlocker.CanChangeDirection(human), Is.False);
                    Assert.That(standingState.Down(human), Is.False);
                    Assert.That(
                        (xformSystem.GetWorldPosition(human) - xformSystem.GetWorldPosition(chair)).LengthSquared,
                        Is.LessThanOrEqualTo(0)
                    );

                    // Side effects of buckling for the strap
                    Assert.That(strap.BuckledEntities, Does.Contain(human));
                    Assert.That(strap.OccupiedSize, Is.EqualTo(buckle.Size));
                    Assert.That(strap.OccupiedSize, Is.Positive);
                });

#pragma warning disable NUnit2045 // Interdependent asserts.
                // Trying to buckle while already buckled fails
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle), Is.False);

                // Trying to unbuckle too quickly fails
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckle.Buckled);
                // Still buckled
#pragma warning restore NUnit2045

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));

                    // Unbuckle, strap
                    Assert.That(strap.BuckledEntities, Is.Empty);
                    Assert.That(strap.OccupiedSize, Is.Zero);
                });

#pragma warning disable NUnit2045 // Interdependent asserts.
                // Re-buckling has no cooldown
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.That(buckle.Buckled);

                // On cooldown
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
#pragma warning disable NUnit2045 // Interdependent asserts.
                // Still buckled
                Assert.That(buckle.Buckled);

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.That(buckle.Buckled, Is.False);
#pragma warning restore NUnit2045

                // Move away from the chair
                var xformQuery = entityManager.GetEntityQuery<TransformComponent>();
                var oldWorldPosition = xformSystem.GetWorldPosition(chair, xformQuery);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(1000, 1000), xformQuery);

                // Out of range
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.False);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
#pragma warning restore NUnit2045

                // Move near the chair
                oldWorldPosition = xformSystem.GetWorldPosition(chair, xformQuery);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(0.5f, 0), xformQuery);

                // In range
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045

                // Force unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));
                });

                // Re-buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));

                // Move away from the chair
                oldWorldPosition = xformSystem.GetWorldPosition(chair, xformQuery);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(1, 0), xformQuery);
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

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task BuckledDyingDropItemsTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            EntityUid human = default;
            BuckleComponent buckle = null;
            HandsComponent hands = null;
            BodyComponent body = null;

            await server.WaitIdleAsync();

            var entityManager = server.ResolveDependency<IEntityManager>();
            var handsSys = entityManager.EntitySysManager.GetEntitySystem<SharedHandsSystem>();
            var buckleSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBuckleSystem>();
            var xformSystem = entityManager.System<SharedTransformSystem>();

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                var chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(human, out buckle));
                    Assert.That(entityManager.HasComponent<StrapComponent>(chair));
                    Assert.That(entityManager.TryGetComponent(human, out hands));
                    Assert.That(entityManager.TryGetComponent(human, out body));
                });

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = entityManager.SpawnEntity(ItemDummyId, coordinates);

                    Assert.That(handsSys.TryPickupAnyHand(human, akms));
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled);

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
                    xformSystem.DetachParentToNull(leg.Id, entityManager.GetComponent<TransformComponent>(leg.Id));
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled);

                // Now with no item in any hand
                foreach (var hand in hands.Hands.Values)
                {
                    Assert.That(hand.HeldEntity, Is.Null);
                }

                buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle);
            });

            await pair.CleanReturnAsync();
        }

        [Test]
        public async Task ForceUnbuckleBuckleTest()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();
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
                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(human, out buckle));
                    Assert.That(entityManager.HasComponent<StrapComponent>(chair));
                });

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });

                // Move the buckled entity away
                var xformQuery = entityManager.GetEntityQuery<TransformComponent>();
                var oldWorldPosition = xformSystem.GetWorldPosition(chair, xformQuery);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(100, 0), xformQuery);
            });

            await PoolManager.WaitUntil(server, () => !buckle.Buckled, 10);

            Assert.That(buckle.Buckled, Is.False);

            await server.WaitAssertion(() =>
            {
                // Move the now unbuckled entity back onto the chair
                var xformQuery = entityManager.GetEntityQuery<TransformComponent>();
                var oldWorldPosition = xformSystem.GetWorldPosition(chair, xformQuery);
                xformSystem.SetWorldPosition(human, oldWorldPosition, xformQuery);

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });
            });

            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });
            });
            await pair.CleanReturnAsync();
        }
    }
}
