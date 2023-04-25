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
        public async Task BuckleUnbuckleCooldownRangeTest()
        {
            await using var pairTracker =
                await PoolManager.GetServerClient(new PoolSettings {ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var actionBlocker = entityManager.EntitySysManager.GetEntitySystem<ActionBlockerSystem>();
            var buckleSystem = entityManager.EntitySysManager.GetEntitySystem<SharedBuckleSystem>();
            var standingState = entityManager.EntitySysManager.GetEntitySystem<StandingStateSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Default state, unbuckled
                Assert.True(entityManager.TryGetComponent(human, out buckle));
                Assert.NotNull(buckle);
                Assert.Null(buckle.BuckledTo);
                Assert.False(buckle.Buckled);
                Assert.True(actionBlocker.CanMove(human));
                Assert.True(actionBlocker.CanChangeDirection(human));
                Assert.True(standingState.Down(human));
                Assert.True(standingState.Stand(human));

                // Default state, no buckled entities, strap
                Assert.True(entityManager.TryGetComponent(chair, out strap));
                Assert.NotNull(strap);
                Assert.IsEmpty(strap.BuckledEntities);
                Assert.Zero(strap.OccupiedSize);

                // Side effects of buckling
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckle));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                Assert.False(actionBlocker.CanMove(human));
                Assert.False(actionBlocker.CanChangeDirection(human));
                Assert.False(standingState.Down(human));
                Assert.That(
                    (entityManager.GetComponent<TransformComponent>(human).WorldPosition -
                     entityManager.GetComponent<TransformComponent>(chair).WorldPosition).Length,
                    Is.LessThanOrEqualTo(0));

                // Side effects of buckling for the strap
                Assert.That(strap.BuckledEntities, Does.Contain(human));
                Assert.That(strap.OccupiedSize, Is.EqualTo(buckle.Size));
                Assert.Positive(strap.OccupiedSize);

                // Trying to buckle while already buckled fails
                Assert.False(buckleSystem.TryBuckle(human, human, chair, buckle));

                // Trying to unbuckle too quickly fails
                Assert.False(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.True(buckle.Buckled);
                Assert.False(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle));
                Assert.True(buckle.Buckled);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Unbuckle
                Assert.True(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.Null(buckle.BuckledTo);
                Assert.False(buckle.Buckled);
                Assert.True(actionBlocker.CanMove(human));
                Assert.True(actionBlocker.CanChangeDirection(human));
                Assert.True(standingState.Down(human));

                // Unbuckle, strap
                Assert.IsEmpty(strap.BuckledEntities);
                Assert.Zero(strap.OccupiedSize);

                // Re-buckling has no cooldown
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.True(buckle.Buckled);

                // On cooldown
                Assert.False(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.True(buckle.Buckled);
                Assert.False(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle));
                Assert.True(buckle.Buckled);
                Assert.False(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle));
                Assert.True(buckle.Buckled);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Unbuckle
                Assert.True(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.False(buckle.Buckled);

                // Move away from the chair
                entityManager.GetComponent<TransformComponent>(human).WorldPosition += (1000, 1000);

                // Out of range
                Assert.False(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.False(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.False(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle));

                // Move near the chair
                entityManager.GetComponent<TransformComponent>(human).WorldPosition =
                    entityManager.GetComponent<TransformComponent>(chair).WorldPosition + (0.5f, 0);

                // In range
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.True(buckle.Buckled);
                Assert.False(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.True(buckle.Buckled);
                Assert.False(buckleSystem.ToggleBuckle(human, human, chair, buckle: buckle));
                Assert.True(buckle.Buckled);

                // Force unbuckle
                Assert.True(buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle));
                Assert.False(buckle.Buckled);
                Assert.True(actionBlocker.CanMove(human));
                Assert.True(actionBlocker.CanChangeDirection(human));
                Assert.True(standingState.Down(human));

                // Re-buckle
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));

                // Move away from the chair
                entityManager.GetComponent<TransformComponent>(human).WorldPosition += (1, 0);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // No longer buckled
                Assert.False(buckle.Buckled);
                Assert.Null(buckle.BuckledTo);
                Assert.IsEmpty(strap.BuckledEntities);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task BuckledDyingDropItemsTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
                {NoClient = true, ExtraPrototypes = Prototypes});
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
                Assert.True(entityManager.TryGetComponent(human, out buckle));
                Assert.True(entityManager.HasComponent<StrapComponent>(chair));
                Assert.True(entityManager.TryGetComponent(human, out hands));
                Assert.True(entityManager.TryGetComponent(human, out body));

                // Buckle
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = entityManager.SpawnEntity(ItemDummyId, coordinates);

                    Assert.True(handsSys.TryPickupAnyHand(human, akms));
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // With items in all hands
                foreach (var hand in hands.Hands.Values)
                {
                    Assert.NotNull(hand.HeldEntity);
                }

                var bodySystem = entityManager.System<BodySystem>();
                var legs = bodySystem.GetBodyChildrenOfType(body.Owner, BodyPartType.Leg, body);

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
                Assert.True(buckle.Buckled);

                // Now with no item in any hand
                foreach (var hand in hands.Hands.Values)
                {
                    Assert.Null(hand.HeldEntity);
                }

                buckleSystem.TryUnbuckle(human, human, true, buckleComp: buckle);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task ForceUnbuckleBuckleTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
                {NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);
            var coordinates = testMap.GridCoords;
            var entityManager = server.ResolveDependency<IEntityManager>();
            var buckleSystem = entityManager.System<SharedBuckleSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;

            await server.WaitAssertion(() =>
            {
                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.True(entityManager.TryGetComponent(human, out buckle));
                Assert.True(entityManager.HasComponent<StrapComponent>(chair));

                // Buckle
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                // Move the buckled entity away
                entityManager.GetComponent<TransformComponent>(human).WorldPosition += (100, 0);
            });

            await PoolManager.WaitUntil(server, () => !buckle.Buckled, 10);

            Assert.False(buckle.Buckled);

            await server.WaitAssertion(() =>
            {
                // Move the now unbuckled entity back onto the chair
                entityManager.GetComponent<TransformComponent>(human).WorldPosition -= (100, 0);

                // Buckle
                Assert.True(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);
            });

            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
