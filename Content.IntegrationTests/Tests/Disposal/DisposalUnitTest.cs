#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.GameObjects.Components;
using Content.Server.Power.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;

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
                var insertTask = unit.TryInsert(entity);
                Assert.That(unit.CanInsert(entity), Is.EqualTo(result));
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

            Assert.That(result, Is.EqualTo(unit.TryFlush()));
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
    damagePrototype: biologicalDamageContainer

- type: entity
  name: WrenchDummy
  id: WrenchDummy
  components:
  - type: Item
  - type: Tool
    qualities:
      - Anchoring

- type: entity
  name: DisposalUnitDummy
  id: DisposalUnitDummy
  components:
  - type: DisposalUnit
  - type: Anchorable
  - type: PowerReceiver
  - type: Physics
    bodyType: Static

- type: entity
  name: DisposalTrunkDummy
  id: DisposalTrunkDummy
  components:
  - type: DisposalEntry
";

        [Test]
        public async Task Test()
        {
            var options = new ServerIntegrationOptions{ExtraPrototypes = Prototypes};
            var server = StartServerDummyTicker(options);

            IEntity human;
            IEntity wrench;
            DisposalUnitComponent unit;

            server.Assert(async () =>
            {
                var mapManager = IoCManager.Resolve<IMapManager>();

                mapManager.CreateNewMapEntity(MapId.Nullspace);

                var entityManager = IoCManager.Resolve<IEntityManager>();

                // Spawn the entities
                human = entityManager.SpawnEntity("HumanDummy", MapCoordinates.Nullspace);
                wrench = entityManager.SpawnEntity("WrenchDummy", MapCoordinates.Nullspace);
                var disposalUnit = entityManager.SpawnEntity("DisposalUnitDummy", MapCoordinates.Nullspace);
                var disposalTrunk = entityManager.SpawnEntity("DisposalTrunkDummy", disposalUnit.Transform.MapPosition);

                // Test for components existing
                Assert.True(disposalUnit.TryGetComponent(out unit!));
                Assert.True(disposalTrunk.HasComponent<DisposalEntryComponent>());

                // Can't insert, unanchored and unpowered
                var physics = disposalUnit.GetComponent<IPhysBody>();
                physics.BodyType = BodyType.Dynamic;
                Assert.False(unit.Anchored);
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);

                // Anchor the disposal unit
                physics.BodyType = BodyType.Static;

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
                Assert.True(disposalUnit.TryGetComponent(out PowerReceiverComponent? power));
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
