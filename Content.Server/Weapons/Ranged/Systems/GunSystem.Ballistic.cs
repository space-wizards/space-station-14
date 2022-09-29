using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void Cycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        EntityUid? ent = null;

        // TODO: Combine with TakeAmmo
        if (component.Entities.Count > 0)
        {
            var existing = component.Entities[^1];
            component.Entities.RemoveAt(component.Entities.Count - 1);

            component.Container.Remove(existing);
            EnsureComp<AmmoComponent>(existing);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            ent = Spawn(component.FillProto, coordinates);
            EnsureComp<AmmoComponent>(ent.Value);
        }

        if (ent != null)
            EjectCartridge(ent.Value);
    }
}
