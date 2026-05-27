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

namespace Content.IntegrationTests.Tests.Disposal;

[TestOf(typeof(DisposalHolderComponent))]
[TestOf(typeof(DisposalEntryComponent))]
[TestOf(typeof(DisposalUnitComponent))]
public sealed class DisposalUnitTest : GameTest
{
    private const string HumanDisposalDummy = "HumanDisposalDummy";
    private const string WrenchDummy = "WrenchDummy";
    private const string DisposalUnitDummy = "DisposalUnitDummy";
    private const string DisposalTrunkDummy = "DisposalTrunkDummy";

    [SidedDependency(Side.Server)] private SharedTransformSystem _sXformSystem = null!;
    [SidedDependency(Side.Server)] private SharedDisposalUnitSystem _sDisposalSystem = null!;
    [SidedDependency(Side.Server)] private PowerReceiverSystem _sPowerReceiverSystem = null!;

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

    [Test]
    public async Task Test()
    {
        await Pair.CreateTestMap();

        //EntityUid unitUid = default;
        //DisposalUnitComponent unitComponent = default!;

        await Server.WaitAssertion(() =>
        {
            // Spawn the entities
            var coordinates = TestMap!.GridCoords;
            var human = SSpawnAtPosition(HumanDisposalDummy, coordinates);
            var wrench = SSpawnAtPosition(WrenchDummy, coordinates);
            var disposalUnit = SSpawnAtPosition(DisposalUnitDummy, coordinates);
            var disposalTrunk = SSpawnAtPosition(DisposalTrunkDummy, coordinates);

            // Test for components existing
            var unitEnt = SEntity<DisposalUnitComponent>(disposalUnit);
            Assert.That(disposalTrunk, Has.Comp<DisposalEntryComponent>(Server));

            // Can't insert, unanchored and unpowered
            _sXformSystem.Unanchor(disposalUnit, SComp<TransformComponent>(disposalUnit));
            UnitInsertContains(unitEnt, false, _sDisposalSystem, human, wrench, disposalUnit, disposalTrunk);

            // Anchor the disposal unit
            _sXformSystem.AnchorEntity(disposalUnit, SComp<TransformComponent>(disposalUnit));

            // No power
            Assert.That(_sPowerReceiverSystem.IsPowered(unitEnt), Is.False);

            // Can't insert the trunk or the unit into itself
            UnitInsertContains(unitEnt, false, _sDisposalSystem, disposalUnit, disposalTrunk);

            // Can insert mobs and items
            UnitInsertContains(unitEnt, true, _sDisposalSystem, human, wrench);

            var worldPos = _sXformSystem.GetWorldPosition(disposalTrunk);

            // Move the disposal trunk away
            _sXformSystem.SetWorldPosition(disposalTrunk, worldPos + new Vector2(1, 0));

            // Fail to flush with a mob and an item
            Flush(unitEnt, false, _sDisposalSystem, human, wrench);

            var xform = SComp<TransformComponent>(disposalTrunk);
            var trunkWorldPos = _sXformSystem.GetWorldPosition(disposalUnit);

            // Move the disposal trunk back
            _sXformSystem.SetWorldPosition(disposalTrunk, trunkWorldPos);
            _sXformSystem.AnchorEntity((disposalTrunk, xform));

            // Fail to flush with a mob and an item, no power
            Flush(unitEnt, false, _sDisposalSystem, human, wrench);

            // Remove power need
            Assert.That(STryComp<ApcPowerReceiverComponent>(disposalUnit, out var powerComp));
            _sPowerReceiverSystem.SetNeedsPower(disposalUnit, false);
            powerComp!.Powered = true;

            // Flush with a mob and an item
            Flush(unitEnt, true, _sDisposalSystem, human, wrench);

            // Re-pressurizing
            Flush(unitEnt, false, _sDisposalSystem);
        });
    }

    private static void UnitInsert(Entity<DisposalUnitComponent> unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        foreach (var entity in entities)
        {
            Assert.That(disposalSystem.TryInsert(unit, entity, null), Is.EqualTo(result));
        }
    }

    private static void UnitContains(Entity<DisposalUnitComponent> unit, bool result, params EntityUid[] entities)
    {
        foreach (var entity in entities)
        {
            Assert.That(unit.Comp.Container?.ContainedEntities.Contains(entity), Is.EqualTo(result));
        }
    }

    private static void UnitInsertContains(Entity<DisposalUnitComponent> unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        UnitInsert(unit, result, disposalSystem, entities);
        UnitContains(unit, result, entities);
    }

    private static void Flush(Entity<DisposalUnitComponent> unit, bool result, SharedDisposalUnitSystem disposalSystem, params EntityUid[] entities)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(unit.Comp.Container?.ContainedEntities, Is.SupersetOf(entities));
            Assert.That(entities, Has.Length.EqualTo(unit.Comp.Container?.ContainedEntities.Count));

            Assert.That(result, Is.EqualTo(disposalSystem.TryFlush(unit)));
            Assert.That(result || entities.Length == 0, Is.EqualTo(unit.Comp.Container?.ContainedEntities.Count == 0));
        }
    }
}
