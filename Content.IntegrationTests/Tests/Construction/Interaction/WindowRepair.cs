#nullable enable
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class WindowRepair : InteractionTest
{
    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    [SidedDependency(Side.Server)] private DamageableSystem _sDamageableSystem = default!;

    [Test]
    public async Task RepairReinforcedWindow()
    {
        await SpawnTarget(ReinforcedWindow);
        var damageableEnt = SEntity<DamageableComponent>(STarget.Value);

        // Damage the entity.
        var damageType = SProtoMan.Index(BluntDamageType);
        var damage = new DamageSpecifier(damageType, FixedPoint2.New(10));
        Assert.That(_sDamageableSystem.GetPositiveDamage(damageableEnt).AnyPositive(), Is.False,
            "Target was already damaged.");
        await Server.WaitPost(() => _sDamageableSystem.TryChangeDamage(damageableEnt.AsNullable(), damage, ignoreResistances: true));
        await RunTicks(5);
        Assert.That(_sDamageableSystem.GetPositiveDamage(damageableEnt).AnyPositive(), Is.True,
            "Target did not take damage.");

        // Repair the entity
        await InteractUsing(Weld);
        Assert.That(_sDamageableSystem.GetPositiveDamage(damageableEnt).AnyPositive(), Is.False,
            "Target was still damaged after welding.");

        // Validate that we can still deconstruct the entity (i.e., that welding deconstruction is not blocked).
        await Interact(
            Weld,
            Screw,
            Pry,
            Weld,
            Screw,
            Wrench);
        AssertDeleted();
        await AssertEntityLookup((RGlass, 2));
    }
}

