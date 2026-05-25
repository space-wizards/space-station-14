#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
using Content.Server.Disposal.Unit;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Disposal.Unit;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Disposal;

[TestOf(typeof(DisposalHolderComponent))]
[TestOf(typeof(DisposalEntryComponent))]
[TestOf(typeof(DisposalUnitComponent))]
public sealed class DisposalUnitTest : GameTest
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
                var insertTransform = Comp<TransformComponent>(toInsert);
                // Not in a tube yet
                Assert.That(insertTransform.ParentUid, Is.EqualTo(unit));
            }, after: [typeof(SharedDisposalUnitSystem)]);
        }
    }

    private const string HumanDisposalDummy = "HumanDisposalDummy";
    private const string WrenchDummy = "WrenchDummy";
    private const string DisposalUnitDummy = "DisposalUnitDummy";
    private const string DisposalTrunkDummy = "DisposalTrunkDummy";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  name: {HumanDisposalDummy}
  id: {HumanDisposalDummy}
  components:
  - type: Body
    prototype: Human
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
  - type: Damageable
  - type: Injurable
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
  name: {WrenchDummy}
  id: {WrenchDummy}
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
  name: {DisposalUnitDummy}
  id: {DisposalUnitDummy}
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
  name: {DisposalTrunkDummy}
  id: {DisposalTrunkDummy}
  components:
  - type: DisposalEntry
  - type: DisposalTube
  - type: Transform
    anchored: true
";

    [SidedDependency(Side.Server)] private SharedTransformSystem _sXformSystem = null!;
    [SidedDependency(Side.Server)] private DisposalUnitSystem _sDisposalSystem = null!;
    [SidedDependency(Side.Server)] private PowerReceiverSystem _sPowerReceiverSystem = null!;

    [Test]
    [Description("Tests basic functionality of disposal units.")]
    public async Task Test()
    {
        await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            // Spawn the entities
            var coordinates = TestMap!.GridCoords;
            var human = SSpawnAtPosition(HumanDisposalDummy, coordinates);
            var wrench = SSpawnAtPosition(WrenchDummy, coordinates);
            var disposalUnit = SSpawnAtPosition(DisposalUnitDummy, coordinates);
            var disposalTrunk = SSpawnAtPosition(DisposalTrunkDummy, coordinates);

            // Test for components existing
            Assert.That(STryComp<DisposalUnitComponent>(disposalUnit, out var unitComponent));
            Assert.That(disposalTrunk, Has.Comp<DisposalEntryComponent>(Server));

            // Can't insert, unanchored and unpowered
            _sXformSystem.Unanchor(disposalUnit);
            UnitInsertContains(disposalUnit, unitComponent!, false, _sDisposalSystem, human, wrench, disposalUnit, disposalTrunk);

            // Anchor the disposal unit
            _sXformSystem.AnchorEntity(disposalUnit);

            // No power
            Assert.That(_sPowerReceiverSystem.IsPowered(disposalUnit), Is.False);

            // Can't insert the trunk or the unit into itself
            UnitInsertContains(disposalUnit, unitComponent!, false, _sDisposalSystem, disposalUnit, disposalTrunk);

            // Can insert mobs and items
            UnitInsertContains(disposalUnit, unitComponent!, true, _sDisposalSystem, human, wrench);

            var trunkWorldPos = _sXformSystem.GetWorldPosition(disposalTrunk);

            // Move the disposal trunk away
            _sXformSystem.SetWorldPosition(disposalTrunk, trunkWorldPos + new Vector2(1, 0));

            // Fail to flush with a mob and an item
            Flush(disposalUnit, unitComponent!, false, _sDisposalSystem, human, wrench);

            var unitWorldPos = _sXformSystem.GetWorldPosition(disposalUnit);

            // Move the disposal trunk back
            _sXformSystem.SetWorldPosition(disposalTrunk, trunkWorldPos);
            _sXformSystem.AnchorEntity(disposalTrunk);

            // Fail to flush with a mob and an item, no power
            Flush(disposalUnit, unitComponent!, false, _sDisposalSystem, human, wrench);

            // Remove power need
            Assert.That(STryComp(disposalUnit, out ApcPowerReceiverComponent? powerComp));
            _sPowerReceiverSystem.SetNeedsPower(disposalUnit, false);
            powerComp!.Powered = true;

            // Flush with a mob and an item
            Flush(disposalUnit, unitComponent!, true, _sDisposalSystem, human, wrench);

            // Re-pressurizing
            Flush(disposalUnit, unitComponent!, false, _sDisposalSystem);
        });
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
        if (result)
        {
            Assert.That(unit.Container.ContainedEntities, Is.SupersetOf(entities));
        }
        else
        {
            Assert.That(unit.Container.ContainedEntities, Is.Not.SupersetOf(entities));
        }
    }

    private static void UnitInsertContains(EntityUid uid, DisposalUnitComponent unit, bool result, DisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        UnitInsert(uid, unit, result, disposalSystem, entities);
        UnitContains(unit, result, entities);
    }

    private static void Flush(EntityUid unitEntity, DisposalUnitComponent unit, bool result, DisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(unit.Container.ContainedEntities, Is.SupersetOf(entities));
            Assert.That(entities, Has.Length.EqualTo(unit.Container.ContainedEntities.Count));

            Assert.That(result, Is.EqualTo(disposalSystem.TryFlush(unitEntity, unit)));
            Assert.That(result || entities.Length == 0, Is.EqualTo(unit.Container.ContainedEntities.Count == 0));
        }
    }
}
