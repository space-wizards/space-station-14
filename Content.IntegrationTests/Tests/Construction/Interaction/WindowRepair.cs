using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction.Interaction;

public sealed class WindowRepair : InteractionTest
{
    [Test]
    public async Task RepairReinforcedWindow()
    {
        await SpawnTarget("ReinforcedWindow");

        // Damage the entity.
        var sys = SEntMan.System<DamageableSystem>();
        var comp = Comp<DamageableComponent>();
        var damageType = Server.ResolveDependency<IPrototypeManager>().Index<DamageTypePrototype>("Blunt");
        var damage = new DamageSpecifier(damageType, FixedPoint2.New(10));
        Assert.That(comp.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));
        await Server.WaitPost(() => sys.TryChangeDamage(SEntMan.GetEntity(Target), damage, ignoreResistances: true));
        await RunTicks(5);
        Assert.That(comp.Damage.GetTotal(), Is.GreaterThan(FixedPoint2.Zero));

        // Repair the entity
        await Interact(Weld);
        Assert.That(comp.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));

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

