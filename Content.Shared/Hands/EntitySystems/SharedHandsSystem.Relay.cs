using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem : EntitySystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<SharedHandsComponent, DamageChangedEvent>(RelayHandItemsEvent);
    }

    private void RelayHandItemsEvent<T>(EntityUid uid, SharedHandsComponent component, T args) where T : EntityEventArgs
    {
        var items = EnumerateHeld(uid, component);

        foreach (var item in items)
        {
            //A bit singleton but I don't want other destructable items in hand
            //To be effected
            if (HasComp<BlockingComponent>(item))
            {
                RaiseLocalEvent(item, args, false);
            }
        }

    }
}
