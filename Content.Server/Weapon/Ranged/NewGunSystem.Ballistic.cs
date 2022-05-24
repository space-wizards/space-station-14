using Content.Shared.Weapons.Ranged;
using Robust.Shared.Map;

namespace Content.Server.Weapon.Ranged;

public sealed partial class NewGunSystem
{
    public override void ManualCycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates)
    {
        EntityUid? ent = null;

        if (component.Cycled)
        {
            // TODO: Combine with TakeAmmo
            if (component.Entities.TryPop(out var existing))
            {
                component.Container.Remove(existing);
                EnsureComp<NewAmmoComponent>(existing);
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                ent = Spawn(component.FillProto, coordinates);
                EnsureComp<NewAmmoComponent>(ent.Value);
            }
        }

        component.Cycled = component.AutoCycle;

        if (ent != null)
        {
            EjectCartridge(ent.Value);
        }
    }
}
