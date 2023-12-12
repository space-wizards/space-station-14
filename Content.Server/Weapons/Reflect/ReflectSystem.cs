using Content.Shared.Item;
using Content.Shared.Weapons.Reflect;

namespace Content.Server.Weapons.Reflect;

public sealed class ReflectSystem : SharedReflectSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReflectComponent, ItemToggleActivatedEvent>(EnableReflect);
        SubscribeLocalEvent<ReflectComponent, ItemToggleDeactivatedEvent>(DisableReflect);
    }

    private void EnableReflect(EntityUid uid, ReflectComponent comp, ref ItemToggleActivatedEvent args)
    {
        comp.Enabled = true;
        Dirty(uid, comp);
    }

    private void DisableReflect(EntityUid uid, ReflectComponent comp, ref ItemToggleDeactivatedEvent args)
    {
        comp.Enabled = false;
        Dirty(uid, comp);
    }
}
