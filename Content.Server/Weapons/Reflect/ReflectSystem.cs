using Content.Server.Weapons.Melee.EnergySword;
using Content.Server.Weapons.Melee.ItemToggle;
using Content.Shared.Weapons.Reflect;

namespace Content.Server.Weapons.Reflect;

public sealed class ReflectSystem : SharedReflectSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReflectComponent, EnergySwordActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergySwordDeactivatedEvent>(DisableReflect);
        SubscribeLocalEvent<ReflectComponent, ItemToggleActivatedEvent>(ShieldEnableReflect);
        SubscribeLocalEvent<ReflectComponent, ItemToggleDeactivatedEvent>(ShieldDisableReflect);
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

    private void ShieldEnableReflect(EntityUid uid, ReflectComponent comp, ref ItemToggleActivatedEvent args)
    {
        comp.Enabled = true;
        Dirty(comp);
    }

    private void ShieldDisableReflect(EntityUid uid, ReflectComponent comp, ref ItemToggleDeactivatedEvent args)
    {
        comp.Enabled = false;
        Dirty(comp);
    }
}
