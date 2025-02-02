using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        EntityUid? ent = null;

        // TODO: Combine with TakeAmmo
        if (component.Entities.Count > 0)
        {
            ent = component.Entities[^1];
            component.Entities.RemoveAt(component.Entities.Count - 1);
            DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.Entities));
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
            ent = Spawn(component.Proto, coordinates);
            Containers.Insert(ent.Value, component.Container);
        }

        if (ent != null)
        {
            Containers.Remove(ent.Value, component.Container);
            EnsureShootable(ent.Value);
            EjectCartridge(ent.Value);
        }

        var cycledEvent = new GunCycledEvent();
        RaiseLocalEvent(uid, ref cycledEvent);
    }
}
