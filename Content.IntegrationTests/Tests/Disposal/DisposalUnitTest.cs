#nullable enable annotations
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Disposal;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Disposal
{
    [TestFixture]
    [TestOf(typeof(DisposalHolderComponent))]
    [TestOf(typeof(DisposalEntryComponent))]
    [TestOf(typeof(DisposalUnitComponent))]
    public sealed class DisposalUnitTest
    {
        [Reflect(false)]
        private sealed class DisposalUnitTestSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<DoInsertDisposalUnitEvent>(ev =>
                {
                    var (_, toInsert, unit) = ev;
                    var insertTransform = EntityManager.GetComponent<TransformComponent>(toInsert);
                    var unitTransform = EntityManager.GetComponent<TransformComponent>(unit);
                    // Not in a tube yet
                    Assert.That(insertTransform.Parent, Is.EqualTo(unitTransform));
                }, after: new[] {typeof(SharedDisposalUnitSystem)});
            }
        }

        private void UnitInsert(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
        {
            var system = EntitySystem.Get<DisposalUnitSystem>();

            foreach (var entity in entities)
            {
                Assert.That(system.CanInsert(unit, entity), Is.EqualTo(result));
                system.TryInsert(unit.Owner, entity, null);
            }
        }

        private void UnitContains(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(unit.Container.ContainedEntities.Contains(entity), Is.EqualTo(result));
            }
        }

        private void UnitInsertContains(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
        {
            UnitInsert(unit, result, entities);
            UnitContains(unit, result, entities);
        }

        private void Flush(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
        {
            Assert.That(unit.Container.ContainedEntities, Is.SupersetOf(entities));
            Assert.That(entities.Length, Is.EqualTo(unit.Container.ContainedEntities.Count));

            Assert.That(result, Is.EqualTo(EntitySystem.Get<DisposalUnitSystem>().TryFlush(unit)));
            Assert.That(result || entities.Length == 0, Is.EqualTo(unit.Container.ContainedEntities.Count == 0));
        }

        private const string Prototypes = @"
- type: entity
  name: HumanDummy
  id: HumanDummy
  components:
  - type: Body
    prototype: Human
  - type: MobState
  - type: Damageable
    damageContainer: Biological
  - type: Physics
    bodyType: KinematicController
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeCircle
        radius: 0.35
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
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeCircle
        radius: 0.35
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
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeCircle
        radius: 0.35

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
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings
                {NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            EntityUid human = default!;
            EntityUid wrench = default!;
            EntityUid disposalUnit = default!;
            EntityUid disposalTrunk = default!;
            DisposalUnitComponent unit = default!;

            var entityManager = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                // Spawn the entities
                var coordinates = testMap.GridCoords;
                human = entityManager.SpawnEntity("HumanDummy", coordinates);
                wrench = entityManager.SpawnEntity("WrenchDummy", coordinates);
                disposalUnit = entityManager.SpawnEntity("DisposalUnitDummy", coordinates);
                disposalTrunk = entityManager.SpawnEntity("DisposalTrunkDummy",
                    IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(disposalUnit).MapPosition);

                // Test for components existing
                ref DisposalUnitComponent? comp = ref unit!;
                Assert.True(entityManager.TryGetComponent(disposalUnit, out comp));
                Assert.True(entityManager.HasComponent<DisposalEntryComponent>(disposalTrunk));

                // Can't insert, unanchored and unpowered
                entityManager.GetComponent<TransformComponent>(unit!.Owner).Anchored = false;
                UnitInsertContains(unit, false, human, wrench, disposalUnit, disposalTrunk);
            });

            await server.WaitAssertion(() =>
            {
                // Anchor the disposal unit
                entityManager.GetComponent<TransformComponent>(unit.Owner).Anchored = true;

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
                entityManager.GetComponent<TransformComponent>(disposalTrunk).WorldPosition += (1, 0);

                // Fail to flush with a mob and an item
                Flush(unit, false, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Move the disposal trunk back
                entityManager.GetComponent<TransformComponent>(disposalTrunk).WorldPosition -= (1, 0);

                // Fail to flush with a mob and an item, no power
                Flush(unit, false, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Remove power need
                Assert.True(entityManager.TryGetComponent(disposalUnit, out ApcPowerReceiverComponent power));
                power!.NeedsPower = false;
                unit.Powered = true; //Power state changed event doesn't get fired smh

                // Flush with a mob and an item
                Flush(unit, true, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Re-pressurizing
                Flush(unit, false);
            });
            await pairTracker.CleanReturnAsync();
        }
    }
}
