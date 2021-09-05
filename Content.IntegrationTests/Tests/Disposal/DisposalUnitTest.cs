#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Coordinates;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Disposal
{
    [TestFixture]
    [TestOf(typeof(DisposalHolderComponent))]
    [TestOf(typeof(DisposalEntryComponent))]
    [TestOf(typeof(DisposalUnitComponent))]
    public class DisposalUnitTest : ContentIntegrationTest
    {
        private void UnitInsert(DisposalUnitComponent unit, bool result, params IEntity[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(EntitySystem.Get<DisposalUnitSystem>().CanInsert(unit, entity), Is.EqualTo(result));
                var insertTask = unit.TryInsert(entity);
                insertTask.ContinueWith(task =>
                {
                    Assert.That(task.Result, Is.EqualTo(result));
                    if (result)
                    {
                        // Not in a tube yet
                        Assert.That(entity.Transform.Parent, Is.EqualTo(unit.Owner.Transform));
                    }
                });
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

- type: entity
  name: DisposalUnitDummy
  id: DisposalUnitDummy
  components:
  - type: DisposalUnit
  - type: Anchorable
  - type: ApcPowerReceiver
  - type: Physics
    bodyType: Static

- type: entity
  name: DisposalTrunkDummy
  id: DisposalTrunkDummy
  components:
  - type: DisposalEntry
  - type: SnapGrid
";

        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);
            await server.WaitIdleAsync();

            IEntity human;
            IEntity wrench;
            DisposalUnitComponent unit;
            EntityCoordinates coordinates = default!;
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var pauseManager = server.ResolveDependency<IPauseManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();
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
                var disposalUnit = entityManager.SpawnEntity("DisposalUnitDummy", coordinates);
                var disposalTrunk = entityManager.SpawnEntity("DisposalTrunkDummy", disposalUnit.Transform.MapPosition);

                // Check that we have a grid, so that we can anchor our unit
                Assert.That(mapManager.TryFindGridAt(disposalUnit.Transform.MapPosition, out var _));

                // Test for components existing
                Assert.True(disposalUnit.TryGetComponent(out unit!));
                Assert.True(disposalTrunk.HasComponent<DisposalEntryComponent>());

                // Can't insert, unanchored and unpowered
                unit.Owner.Transform.Anchored = false;
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);

                // Anchor the disposal unit
                unit.Owner.Transform.Anchored = true;

                // No power
                Assert.False(unit.Powered);

                // Can't insert the trunk or the unit into itself
                UnitInsertContains(unit, false, disposalUnit, disposalTrunk);

                // Can insert mobs and items
                UnitInsertContains(unit, true, human, wrench);

                // Move the disposal trunk away
                disposalTrunk.Transform.WorldPosition += (1, 0);

                // Fail to flush with a mob and an item
                Flush(unit, false, human, wrench);

                // Move the disposal trunk back
                disposalTrunk.Transform.WorldPosition -= (1, 0);

                // Fail to flush with a mob and an item, no power
                Flush(unit, false, human, wrench);

                // Remove power need
                Assert.True(disposalUnit.TryGetComponent(out ApcPowerReceiverComponent? power));
                power!.NeedsPower = false;
                Assert.True(unit.Powered);

                // Flush with a mob and an item
                Flush(unit, true, human, wrench);

                // Re-pressurizing
                Flush(unit, false);
            });

            await server.WaitIdleAsync();
        }
    }
}
