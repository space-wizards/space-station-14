#nullable enable annotations
using System.Linq;
using System.Numerics;
using Content.Server.Disposal.Tube.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
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
                    Assert.That(insertTransform.ParentUid, Is.EqualTo(unit));
                }, after: new[] { typeof(SharedDisposalUnitSystem) });
            }
        }

        private static void UnitInsert(EntityUid uid, DisposalUnitComponent unit, bool result, DisposalUnitSystem disposalSystem, params EntityUid[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(disposalSystem.CanInsert(uid, unit, entity), Is.EqualTo(result));
                disposalSystem.TryInsert(uid, entity, null);
            }
        }

        private static void UnitContains(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
        {
            foreach (var entity in entities)
            {
                Assert.That(unit.Container.ContainedEntities.Contains(entity), Is.EqualTo(result));
            }
        }

        private static void UnitInsertContains(EntityUid uid, DisposalUnitComponent unit, bool result, DisposalUnitSystem disposalSystem, params EntityUid[] entities)
        {
            UnitInsert(uid, unit, result, disposalSystem, entities);
            UnitContains(unit, result, entities);
        }

        private static void Flush(EntityUid unitEntity, DisposalUnitComponent unit, bool result, DisposalUnitSystem disposalSystem, params EntityUid[] entities)
        {
            Assert.Multiple(() =>
            {
                Assert.That(unit.Container.ContainedEntities, Is.SupersetOf(entities));
                Assert.That(entities, Has.Length.EqualTo(unit.Container.ContainedEntities.Count));

                Assert.That(result, Is.EqualTo(disposalSystem.TryFlush(unitEntity, unit)));
                Assert.That(result || entities.Length == 0, Is.EqualTo(unit.Container.ContainedEntities.Count == 0));
            });
        }

        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanDisposalDummy
  id: HumanDisposalDummy
  components:
  - type: Body
    prototype: Human
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
  - type: Damageable
    damageContainer: Biological
  - type: Physics
    bodyType: KinematicController
  - type: Fixtures
    fixtures:
      fix1:
        shape:
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
      fix1:
        shape:
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
    flushTime: 0
  - type: Anchorable
  - type: ApcPowerReceiver
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
          radius: 0.35

- type: entity
  name: DisposalTrunkDummy
  id: DisposalTrunkDummy
  components:
  - type: DisposalEntry
  - type: DisposalTube
  - type: Transform
    anchored: true
";

        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;

            var testMap = await pair.CreateTestMap();

            EntityUid human = default!;
            EntityUid wrench = default!;
            EntityUid disposalUnit = default!;
            EntityUid disposalTrunk = default!;

            EntityUid unitUid = default;
            DisposalUnitComponent unitComponent = default!;

            var entityManager = server.ResolveDependency<IEntityManager>();
            var xformSystem = entityManager.System<SharedTransformSystem>();
            var disposalSystem = entityManager.System<DisposalUnitSystem>();
            await server.WaitAssertion(() =>
            {
                // Spawn the entities
                var coordinates = testMap.GridCoords;
                human = entityManager.SpawnEntity("HumanDisposalDummy", coordinates);
                wrench = entityManager.SpawnEntity("WrenchDummy", coordinates);
                disposalUnit = entityManager.SpawnEntity("DisposalUnitDummy", coordinates);
                disposalTrunk = entityManager.SpawnEntity("DisposalTrunkDummy", coordinates);

                // Test for components existing
                unitUid = disposalUnit;
                Assert.Multiple(() =>
                {
                    Assert.That(entityManager.TryGetComponent(disposalUnit, out unitComponent));
                    Assert.That(entityManager.HasComponent<DisposalEntryComponent>(disposalTrunk));
                });

                // Can't insert, unanchored and unpowered
                xformSystem.Unanchor(unitUid, entityManager.GetComponent<TransformComponent>(unitUid));
                UnitInsertContains(disposalUnit, unitComponent, false, disposalSystem, human, wrench, disposalUnit, disposalTrunk);
            });

            await server.WaitAssertion(() =>
            {
                // Anchor the disposal unit
                xformSystem.AnchorEntity(unitUid, entityManager.GetComponent<TransformComponent>(unitUid));

                // No power
                Assert.That(unitComponent.Powered, Is.False);

                // Can't insert the trunk or the unit into itself
                UnitInsertContains(unitUid, unitComponent, false, disposalSystem, disposalUnit, disposalTrunk);

                // Can insert mobs and items
                UnitInsertContains(unitUid, unitComponent, true, disposalSystem, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                var worldPos = xformSystem.GetWorldPosition(disposalTrunk);

                // Move the disposal trunk away
                xformSystem.SetWorldPosition(disposalTrunk, worldPos + new Vector2(1, 0));

                // Fail to flush with a mob and an item
                Flush(disposalUnit, unitComponent, false, disposalSystem, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                var xform = entityManager.GetComponent<TransformComponent>(disposalTrunk);
                var worldPos = xformSystem.GetWorldPosition(disposalUnit);

                // Move the disposal trunk back
                xformSystem.SetWorldPosition(disposalTrunk, worldPos);
                xformSystem.AnchorEntity((disposalTrunk, xform));

                // Fail to flush with a mob and an item, no power
                Flush(disposalUnit, unitComponent, false, disposalSystem, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Remove power need
                Assert.That(entityManager.TryGetComponent(disposalUnit, out ApcPowerReceiverComponent power));
                power!.NeedsPower = false;
                unitComponent.Powered = true; //Power state changed event doesn't get fired smh

                // Flush with a mob and an item
                Flush(disposalUnit, unitComponent, true, disposalSystem, human, wrench);
            });

            await server.WaitAssertion(() =>
            {
                // Re-pressurizing
                Flush(disposalUnit, unitComponent, false, disposalSystem);
            });

            await pair.CleanReturnAsync();
        }
    }
}
