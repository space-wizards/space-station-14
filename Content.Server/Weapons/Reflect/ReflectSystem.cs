using Content.Server.Weapons.Melee.EnergySword;
using Content.Server.Weapons.Melee.EnergyShield;
using Content.Shared.Weapons.Reflect;

namespace Content.Server.Weapons.Reflect;

public sealed class ReflectSystem : SharedReflectSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReflectComponent, EnergySwordActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergySwordDeactivatedEvent>(DisableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergyShieldActivatedEvent>(ShieldEnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergyShieldDeactivatedEvent>(ShieldDisableReflect);
    }

    private void EnableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordActivatedEvent args)
    {
        comp.Enabled = true;
        Dirty(comp);
    }

    private void DisableReflect(EntityUid uid, ReflectComponent comp, ref EnergySwordDeactivatedEvent args)
    {
        comp.Enabled = false;
        Dirty(comp);
    }

    private void ShieldEnableReflect(EntityUid uid, ReflectComponent comp, ref EnergyShieldActivatedEvent args)
    {
        comp.Enabled = true;
        Dirty(comp);
    }

    private void ShieldDisableReflect(EntityUid uid, ReflectComponent comp, ref EnergyShieldDeactivatedEvent args)
    {
        comp.Enabled = false;
        Dirty(comp);
    }
}
