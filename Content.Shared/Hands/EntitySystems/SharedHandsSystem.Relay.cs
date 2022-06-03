using Content.Shared.Damage;
using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeRelay()
    {
        //SubscribeLocalEvent<SharedHandsComponent, DamageModifyEvent>(RelayHandItemsEvent);
    }

    private void RelayHandItemsEvent<T>(EntityUid uid, SharedHandsComponent component, T args) where T : EntityEventArgs
    {
        var items = EnumerateHeld(uid, component);

        foreach (var item in items)
        {
            RaiseLocalEvent(item, args, false);
        }

    }
}
