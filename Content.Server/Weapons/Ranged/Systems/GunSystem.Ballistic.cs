using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void Cycle(Entity<BallisticAmmoProviderComponent> ent, MapCoordinates coordinates)
    {
        EntityUid? ammoEnt = null;

        // TODO: Combine with TakeAmmo
        if (ent.Comp.Entities.Count > 0)
        {
            var existing = ent.Comp.Entities[^1];
            ent.Comp.Entities.RemoveAt(ent.Comp.Entities.Count - 1);
            DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));

            Containers.Remove(existing, ent.Comp.Container);
            EnsureShootable(existing);
        }
        else if (ent.Comp.UnspawnedCount > 0)
        {
            ent.Comp.UnspawnedCount--;
            DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
            ammoEnt = Spawn(ent.Comp.Proto, coordinates);
            EnsureShootable(ammoEnt.Value);
        }

        if (ammoEnt != null)
            EjectCartridge(ammoEnt.Value);

        var cycledEvent = new GunCycledEvent();
        RaiseLocalEvent(ent, ref cycledEvent);
    }
}
