using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Strap;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Buckle
{
    [TestFixture]
    [TestOf(typeof(BuckleComponent))]
    [TestOf(typeof(StrapComponent))]
    public class BuckleTest : ContentIntegrationTest
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
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso

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
            var cOptions = new ClientIntegrationOptions {ExtraPrototypes = Prototypes};
            var sOptions = new ServerIntegrationOptions {ExtraPrototypes = Prototypes};
            var (_, server) = await StartConnectedServerClientPair(cOptions, sOptions);

            IEntity human = null;
            IEntity chair = null;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var entityManager = IoCManager.Resolve<IEntityManager>();

                var gridId = new GridId(1);
                var grid = mapManager.GetGrid(gridId);
                var coordinates = grid.GridEntityId.ToCoordinates();

                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Default state, unbuckled
                Assert.True(human.TryGetComponent(out buckle));
                Assert.NotNull(buckle);
                Assert.Null(buckle.BuckledTo);
                Assert.False(buckle.Buckled);
                Assert.True(ActionBlockerSystem.CanMove(human));
                Assert.True(ActionBlockerSystem.CanChangeDirection(human));
                Assert.True(EffectBlockerSystem.CanFall(human));

                // Default state, no buckled entities, strap
                Assert.True(chair.TryGetComponent(out strap));
                Assert.NotNull(strap);
                Assert.IsEmpty(strap.BuckledEntities);
                Assert.Zero(strap.OccupiedSize);

                // Side effects of buckling
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                var player = IoCManager.Resolve<IPlayerManager>().GetAllPlayers().Single();
                Assert.True(((BuckleComponentState) buckle.GetComponentState(player)).Buckled);
                Assert.False(ActionBlockerSystem.CanMove(human));
                Assert.False(ActionBlockerSystem.CanChangeDirection(human));
                Assert.False(EffectBlockerSystem.CanFall(human));
                Assert.That(human.Transform.WorldPosition, Is.EqualTo(chair.Transform.WorldPosition));

                // Side effects of buckling for the strap
                Assert.That(strap.BuckledEntities, Does.Contain(human));
                Assert.That(strap.OccupiedSize, Is.EqualTo(buckle.Size));
                Assert.Positive(strap.OccupiedSize);

                // Trying to buckle while already buckled fails
                Assert.False(buckle.TryBuckle(human, chair));

                // Trying to unbuckle too quickly fails
                Assert.False(buckle.TryUnbuckle(human));
                Assert.False(buckle.ToggleBuckle(human, chair));
                Assert.True(buckle.Buckled);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Unbuckle
                Assert.True(buckle.TryUnbuckle(human));
                Assert.Null(buckle.BuckledTo);
                Assert.False(buckle.Buckled);
                Assert.True(ActionBlockerSystem.CanMove(human));
                Assert.True(ActionBlockerSystem.CanChangeDirection(human));
                Assert.True(EffectBlockerSystem.CanFall(human));

                // Unbuckle, strap
                Assert.IsEmpty(strap.BuckledEntities);
                Assert.Zero(strap.OccupiedSize);

                // Re-buckling has no cooldown
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.True(buckle.Buckled);

                // On cooldown
                Assert.False(buckle.TryUnbuckle(human));
                Assert.True(buckle.Buckled);
                Assert.False(buckle.ToggleBuckle(human, chair));
                Assert.True(buckle.Buckled);
                Assert.False(buckle.ToggleBuckle(human, chair));
                Assert.True(buckle.Buckled);
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await server.WaitRunTicks(60);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Unbuckle
                Assert.True(buckle.TryUnbuckle(human));
                Assert.False(buckle.Buckled);

                // Move away from the chair
                human.Transform.WorldPosition += (1000, 1000);

                // Out of range
                Assert.False(buckle.TryBuckle(human, chair));
                Assert.False(buckle.TryUnbuckle(human));
                Assert.False(buckle.ToggleBuckle(human, chair));

                // Move near the chair
                human.Transform.WorldPosition = chair.Transform.WorldPosition + (0.5f, 0);

                // In range
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.True(buckle.Buckled);
                Assert.False(buckle.TryUnbuckle(human));
                Assert.True(buckle.Buckled);
                Assert.False(buckle.ToggleBuckle(human, chair));
                Assert.True(buckle.Buckled);

                // Force unbuckle
                Assert.True(buckle.TryUnbuckle(human, true));
                Assert.False(buckle.Buckled);
                Assert.True(ActionBlockerSystem.CanMove(human));
                Assert.True(ActionBlockerSystem.CanChangeDirection(human));
                Assert.True(EffectBlockerSystem.CanFall(human));

                // Re-buckle
                Assert.True(buckle.TryBuckle(human, chair));

                // Move away from the chair
                human.Transform.WorldPosition += (1, 0);
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                // No longer buckled
                Assert.False(buckle.Buckled);
                Assert.Null(buckle.BuckledTo);
                Assert.IsEmpty(strap.BuckledEntities);
            });
        }

        [Test]
        public async Task BuckledDyingDropItemsTest()
        {
            var options = new ServerContentIntegrationOption {ExtraPrototypes = Prototypes};
            var server = StartServer(options);

            IEntity human = null;
            BuckleComponent buckle = null;
            HandsComponent hands = null;
            IBody body = null;

            await server.WaitIdleAsync();

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var entityManager = IoCManager.Resolve<IEntityManager>();

                var gridId = new GridId(1);
                var grid = mapManager.GetGrid(gridId);
                var coordinates = grid.GridEntityId.ToCoordinates();

                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                IEntity chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.True(human.TryGetComponent(out buckle));
                Assert.True(chair.HasComponent<StrapComponent>());
                Assert.True(human.TryGetComponent(out hands));
                Assert.True(human.TryGetComponent(out body));

                // Buckle
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = entityManager.SpawnEntity(ItemDummyId, coordinates);

                    // Equip items
                    Assert.True(akms.TryGetComponent(out ItemComponent item));
                    Assert.True(hands.PutInHand(item));
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // With items in all hands
                foreach (var slot in hands.HandNames)
                {
                    Assert.NotNull(hands.GetItem(slot));
                }

                var legs = body.GetPartsOfType(BodyPartType.Leg);

                // Break our guy's kneecaps
                foreach (var leg in legs)
                {
                    body.RemovePart(leg);
                }
            });

            await server.WaitRunTicks(10);

            await server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Now with no item in any hand
                foreach (var slot in hands.HandNames)
                {
                    Assert.Null(hands.GetItem(slot));
                }

                buckle.TryUnbuckle(human, true);
            });
        }

        [Test]
        public async Task ForceUnbuckleBuckleTest()
        {
            var options = new ServerContentIntegrationOption
            {
                ExtraPrototypes = Prototypes
            };
            var server = StartServer(options);

            IEntity human = null;
            IEntity chair = null;
            BuckleComponent buckle = null;

            await server.WaitAssertion(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();
                var entityManager = IoCManager.Resolve<IEntityManager>();

                var gridId = new GridId(1);
                var grid = mapManager.GetGrid(gridId);
                var coordinates = grid.GridEntityId.ToCoordinates();

                human = entityManager.SpawnEntity(BuckleDummyId, coordinates);
                chair = entityManager.SpawnEntity(StrapDummyId, coordinates);

                // Component sanity check
                Assert.True(human.TryGetComponent(out buckle));
                Assert.True(chair.HasComponent<StrapComponent>());

                // Buckle
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                // Move the buckled entity away
                human.Transform.LocalPosition += (100, 0);
            });

            await WaitUntil(server, () => !buckle.Buckled, 10);

            Assert.False(buckle.Buckled);

            await server.WaitAssertion(() =>
            {
                // Move the now unbuckled entity back onto the chair
                human.Transform.LocalPosition -= (100, 0);

                // Buckle
                Assert.True(buckle.TryBuckle(human, chair));
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
        }
    }
}
