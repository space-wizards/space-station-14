using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;

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

        var sound = component.SoundRack?.GetSound();

        if (sound != null)
            SoundSystem.Play(Filter.Pvs(component.Owner, entityManager: EntityManager), sound);

        if (TryComp<AppearanceComponent>(component.Owner, out var appearance))
        {
            appearance.SetData(AmmoVisuals.AmmoCount, GetShots(component));
        }
    }
}
