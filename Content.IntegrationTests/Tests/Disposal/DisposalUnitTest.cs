#nullable enable
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.NUnit.Constraints;
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
public sealed partial class DisposalUnitTest : GameTest
{
    [Reflect(false)]
    private sealed partial class DisposalUnitTestSystem : EntitySystem;

    private static void UnitInsert(EntityUid uid, DisposalUnitComponent unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        foreach (var entity in entities)
        {
            Assert.That(disposalSystem.TryInsert((uid, unit), entity, null), Is.EqualTo(result));
        }
    }

    private static void UnitContains(DisposalUnitComponent unit, bool result, params EntityUid[] entities)
    {
        foreach (var entity in entities)
        {
            Assert.That(unit.Container.ContainedEntities.Contains(entity), Is.EqualTo(result));
        }
    }

    private static void UnitInsertContains(EntityUid uid, DisposalUnitComponent unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        UnitInsert(uid, unit, result, disposalSystem, entities);
        UnitContains(unit, result, entities);
    }

    private static void Flush(EntityUid unitEntity, DisposalUnitComponent unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(unit.Container.ContainedEntities, Is.SupersetOf(entities));
            Assert.That(entities, Has.Length.EqualTo(unit.Container.ContainedEntities.Count));

            Assert.That(result, Is.EqualTo(disposalSystem.TryFlush((unitEntity, unit))));
            Assert.That(result || entities.Length == 0, Is.EqualTo(unit.Container.ContainedEntities.Count == 0));
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
    whitelist:
      components:
      - Item
      - Body
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

    [SidedDependency(Side.Server)] private SharedTransformSystem _sTransformSystem = default!;
    [SidedDependency(Side.Server)] private SharedDisposalUnitSystem _sDisposalUnitSystem = default!;
    [SidedDependency(Side.Server)] private PowerReceiverSystem _sPowerReceiverSystem = default!;

    [Test]
    public async Task Test()
    {
        var testMap = await Pair.CreateTestMap();

        EntityUid human = default!;
        EntityUid wrench = default!;
        EntityUid disposalUnit = default!;
        EntityUid disposalTrunk = default!;

        EntityUid unitUid = default;
        DisposalUnitComponent? unitComponent = default!;

        await Server.WaitAssertion(() =>
        {
            // Spawn the entities
            var coordinates = testMap.GridCoords;
            human = SSpawnAtPosition(HumanDisposalDummy, coordinates);
            wrench = SSpawnAtPosition(WrenchDummy, coordinates);
            disposalUnit = SSpawnAtPosition(DisposalUnitDummy, coordinates);
            disposalTrunk = SSpawnAtPosition(DisposalTrunkDummy, coordinates);

            // Test for components existing
            unitUid = disposalUnit;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(STryComp(disposalUnit, out unitComponent));
                Assert.That(disposalTrunk, Has.Comp<DisposalEntryComponent>(Server));
            }

            // Can't insert, unanchored and unpowered
            _sTransformSystem.Unanchor(unitUid, SComp<TransformComponent>(unitUid));
            UnitInsertContains(disposalUnit, unitComponent!, false, _sDisposalUnitSystem, human, wrench, disposalUnit, disposalTrunk);
        });

        await Server.WaitAssertion(() =>
        {
            // Anchor the disposal unit
            _sTransformSystem.AnchorEntity(unitUid, SComp<TransformComponent>(unitUid));

            // No power
            Assert.That(_sPowerReceiverSystem.IsPowered(unitUid), Is.False);

            // Can't insert the trunk or the unit into itself
            UnitInsertContains(unitUid, unitComponent, false, _sDisposalUnitSystem, disposalUnit, disposalTrunk);

            // Can insert mobs and items
            UnitInsertContains(unitUid, unitComponent, true, _sDisposalUnitSystem, human, wrench);
        });

        await Server.WaitAssertion(() =>
        {
            var worldPos = _sTransformSystem.GetWorldPosition(disposalTrunk);

            // Move the disposal trunk away
            _sTransformSystem.SetWorldPosition(disposalTrunk, worldPos + new Vector2(1, 0));

            // Fail to flush with a mob and an item
            Flush(disposalUnit, unitComponent, false, _sDisposalUnitSystem, human, wrench);
        });

        await Server.WaitAssertion(() =>
        {
            var xform = SComp<TransformComponent>(disposalTrunk);
            var worldPos = _sTransformSystem.GetWorldPosition(disposalUnit);

            // Move the disposal trunk back
            _sTransformSystem.SetWorldPosition(disposalTrunk, worldPos);
            _sTransformSystem.AnchorEntity((disposalTrunk, xform));

            // Fail to flush with a mob and an item, no power
            Flush(disposalUnit, unitComponent, false, _sDisposalUnitSystem, human, wrench);
        });

        await Server.WaitAssertion(() =>
        {
            // Remove power need
            Assert.That(STryComp<ApcPowerReceiverComponent>(disposalUnit, out var powerComp));
            _sPowerReceiverSystem.SetNeedsPower(disposalUnit, false);
            powerComp!.Powered = true;

            // Flush with a mob and an item
            Flush(disposalUnit, unitComponent, true, _sDisposalUnitSystem, human, wrench);
        });

        await Server.WaitAssertion(() =>
        {
            // Re-pressurizing
            Flush(disposalUnit, unitComponent, false, _sDisposalUnitSystem);
        });
    }
}
