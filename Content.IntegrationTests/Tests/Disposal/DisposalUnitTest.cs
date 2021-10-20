using System.Linq;
using System.Threading.Tasks;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Coordinates;
using Content.Shared.Disposal;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Disposal
{
    [TestFixture]
    [TestOf(typeof(DisposalHolderComponent))]
    [TestOf(typeof(DisposalEntryComponent))]
    [TestOf(typeof(DisposalUnitComponent))]
    public class DisposalUnitTest : ContentIntegrationTest
    {
        [Reflect(false)]
        private class DisposalUnitTestSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<DoInsertDisposalUnitEvent>(ev =>
                {
                    var (_, toInsert, unit) = ev;
                    var insertTransform = EntityManager.GetComponent<ITransformComponent>(toInsert);
                    var unitTransform = EntityManager.GetComponent<ITransformComponent>(unit);
                    // Not in a tube yet
                    Assert.That(insertTransform.Parent, Is.EqualTo(unitTransform));
                }, after: new[] {typeof(SharedDisposalUnitSystem)});
            }
        }

        private void UnitInsert(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            var system = EntitySystem.Get<DisposalUnitSystem>();

            foreach (var entity in entities)
            {
                Assert.That(system.CanInsert(unit, entity), Is.EqualTo(result));
                system.TryInsert(unit.Owner.Uid, entity.Uid, entity.Uid);
            }
        }

        private void UnitContains(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(unit.ContainedEntities.Contains(entity), Is.EqualTo(result));
            }
        }

        private void UnitInsertContains(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            UnitInsert(unit, result, entities);
            UnitContains(unit, result, entities);
        }

        private void Flush(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            Assert.That(unit.ContainedEntities, Is.SupersetOf(entities));
            Assert.That(entities.Length, Is.EqualTo(unit.ContainedEntities.Count));

            Assert.That(result, Is.EqualTo(EntitySystem.Get<DisposalUnitSystem>().TryFlush(unit)));
            Assert.That(result || entities.Length == 0, Is.EqualTo(unit.ContainedEntities.Count == 0));
        }

        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Body
  - type: MobState
  - type: Damageable
    damageContainer: Biological
  - type: Physics
    bodyType: KinematicController
  - type: DoAfter

- type: entity
  name: WrenchDummy
  id: WrenchDummy
  components:
  - type: Item
  - type: Tool
    qualities:
      - Anchoring
  - type: Physics
    bodyType: Dynamic
  - type: DoAfter

- type: entity
  name: DisposalUnitDummy
  id: DisposalUnitDummy
  components:
  - type: DisposalUnit
    entryDelay: 0
    draggedEntryDelay: 0
  - type: Anchorable
  - type: ApcPowerReceiver
  - type: Physics
    bodyType: Static

- type: entity
  name: DisposalTrunkDummy
  id: DisposalTrunkDummy
  components:
  - type: DisposalEntry
  - type: Transform
    anchored: true
";

        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions { ExtraPrototypes = Prototypes };
            var server = StartServerDummyTicker(options);
            await server.WaitIdleAsync();

            IEntity human = default!;
            IEntity wrench = default!;
            IEntity disposalUnit = default!;
            IEntity disposalTrunk = default!;
            DisposalUnitComponent unit = default!;
            EntityCoordinates coordinates = default!;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            // Build up test environment
            server.Post(() =>
            {
                // Create a one tile grid to anchor our disposal unit to.
                var mapId = mapManager.CreateMap();

                pauseManager.AddUninitializedMap(mapId);

                var gridId = new GridId(1);

                if (!mapManager.TryGetGrid(gridId, out var grid))
                {
                    grid = mapManager.CreateGrid(mapId, gridId);
                }

                var tileDefinition = tileDefinitionManager["underplating"];
                var tile = new Tile(tileDefinition.TileId);
                coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                pauseManager.DoMapInitialize(mapId);
            });

            await server.WaitAssertion(() =>
            {
                // Spawn the entities
                human = entityManager.SpawnEntity("HumanDummy", coordinates);
                wrench = entityManager.SpawnEntity("WrenchDummy", coordinates);
                disposalUnit = entityManager.SpawnEntity("DisposalUnitDummy", coordinates);
                disposalTrunk = entityManager.SpawnEntity("DisposalTrunkDummy", disposalUnit.Transform.MapPosition);

                // Check that we have a grid, so that we can anchor our unit
                Assert.That(mapManager.TryFindGridAt(disposalUnit.Transform.MapPosition, out var _));

                // Test for components existing
                Assert.True(disposalUnit.TryGetComponent(out unit!));
                Assert.True(disposalTrunk.HasComponent<DisposalEntryComponent>());

                // Can't insert, unanchored and unpowered
                unit.Owner.Transform.Anchored = false;
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);
            });

            await server.WaitAssertion(() =>
            {
                // Anchor the disposal unit
                unit.Owner.Transform.Anchored = true;

                // No power
                Assert.False(unit.Powered);

                // Can't insert the trunk or the unit into itself
                UnitInsertContains(unit, false, disposalUnit, disposalTrunk);

                // Can insert mobs and items
                UnitInsertContains(unit, true, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Move the disposal trunk away
                disposalTrunk.Transform.WorldPosition += (1, 0);

                // Fail to flush with a mob and an item
                Flush(unit, false, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Move the disposal trunk back
                disposalTrunk.Transform.WorldPosition -= (1, 0);

                // Fail to flush with a mob and an item, no power
                Flush(unit, false, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Remove power need
                Assert.True(disposalUnit.TryGetComponent(out ApcPowerReceiverComponent power));
                power!.NeedsPower = false;
                Assert.True(unit.Powered);

                // Flush with a mob and an item
                Flush(unit, true, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Re-pressurizing
                Flush(unit, false);
            });
        }
    }
}
