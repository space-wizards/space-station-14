using Content.Server.Weapons.Melee.EnergySword;
using Content.Server.Weapons.Melee.ItemToggle;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Inventory.Events;

namespace Content.Server.Weapons.Reflect;

public sealed class ReflectSystem : SharedReflectSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReflectComponent, GotEquippedEvent>(Enable);
        SubscribeLocalEvent<ReflectComponent, GotUnequippedEvent>(Disable);
        SubscribeLocalEvent<ReflectComponent, EnergySwordActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, EnergySwordDeactivatedEvent>(DisableReflect);
        SubscribeLocalEvent<ReflectComponent, ItemToggleActivatedEvent>(ShieldEnableReflect);
        SubscribeLocalEvent<ReflectComponent, ItemToggleDeactivatedEvent>(ShieldDisableReflect);
    }


    private void Enable(EntityUid uid, ReflectComponent comp, GotEquippedEvent args)
    {
        if (!TryComp(args.Equipee, out ReflectComponent? reflection))
            return;

        reflection.ReflectProb += (1 - reflection.ReflectProb) * comp.ReflectProb;
        Dirty(comp);
    }

    private void Disable(EntityUid uid, ReflectComponent comp, GotUnequippedEvent args)
    {
        if (!TryComp(args.Equipee, out ReflectComponent? reflection))
            return;

        reflection.ReflectProb -= (1 - reflection.ReflectProb) * comp.ReflectProb;
        Dirty(comp);
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
