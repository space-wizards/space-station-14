using Content.Shared.Item;
using Content.Shared.Weapons.Reflect;

namespace Content.Server.Weapons.Reflect;

public sealed class ReflectSystem : SharedReflectSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReflectComponent, ItemToggleReflectUpdateEvent>(ToggleReflect);
    }

    private void ToggleReflect(EntityUid uid, ReflectComponent comp, ref ItemToggleReflectUpdateEvent args)
    {
        comp.Enabled = args.Activated;
        Dirty(uid, comp);
    }
}
