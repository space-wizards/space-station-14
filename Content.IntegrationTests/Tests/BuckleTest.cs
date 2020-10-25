using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Strap;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(BuckleComponent))]
    [TestOf(typeof(StrapComponent))]
    public class BuckleTest : ContentIntegrationTest
    {
        [Test]
        public async Task Test()
        {
            var server = StartServerDummyTicker();

            IEntity human = null;
            IEntity chair = null;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                human = entityManager.SpawnEntity("HumanMob_Content", MapCoordinates.Nullspace);
                chair = entityManager.SpawnEntity("ChairWood", MapCoordinates.Nullspace);

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
                Assert.True(((BuckleComponentState) buckle.GetComponentState()).Buckled);
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
            server.RunTicks(60);

            server.Assert(() =>
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
            server.RunTicks(60);

            server.Assert(() =>
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

            server.RunTicks(1);

            server.Assert(() =>
            {
                // No longer buckled
                Assert.False(buckle.Buckled);
                Assert.Null(buckle.BuckledTo);
                Assert.IsEmpty(strap.BuckledEntities);
            });

            await server.WaitIdleAsync();
        }

        [Test]
        public async Task BuckledDyingDropItemsTest()
        {
            var server = StartServer();

            IEntity human = null;
            IEntity chair = null;
            BuckleComponent buckle = null;
            StrapComponent strap = null;
            HandsComponent hands = null;
            IBody body = null;

            server.Assert(() =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                var mapId = new MapId(1);
                mapManager.CreateNewMapEntity(mapId);

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var gridId = new GridId(1);
                var grid = mapManager.CreateGrid(mapId, gridId);
                var coordinates = grid.GridEntityId.ToCoordinates();
                var tileManager = IoCManager.Resolve<ITileDefinitionManager>();
                var tileId = tileManager["underplating"].TileId;
                var tile = new Tile(tileId);

                grid.SetTile(coordinates, tile);

                human = entityManager.SpawnEntity("HumanMob_Content", coordinates);
                chair = entityManager.SpawnEntity("ChairWood", coordinates);

                // Component sanity check
                Assert.True(human.TryGetComponent(out buckle));
                Assert.True(chair.TryGetComponent(out strap));
                Assert.True(human.TryGetComponent(out hands));
                Assert.True(human.TryGetComponent(out body));

                // Buckle
                Assert.True(buckle.TryBuckle(human, chair));
                Assert.NotNull(buckle.BuckledTo);
                Assert.True(buckle.Buckled);

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = entityManager.SpawnEntity("RifleAk", coordinates);

                    // Equip items
                    Assert.True(akms.TryGetComponent(out ItemComponent item));
                    Assert.True(hands.PutInHand(item));
                }
            });

            server.RunTicks(10);

            server.Assert(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // With items in all hands
                foreach (var slot in hands.Hands)
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

            server.RunTicks(10);

            server.Assert(() =>
            {
                // Still buckled
                Assert.True(buckle.Buckled);

                // Now with no item in any hand
                foreach (var slot in hands.Hands)
                {
                    Assert.Null(hands.GetItem(slot));
                }
            });

            await server.WaitIdleAsync();
        }
    }
}
