using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
    }

    private void OnMagazineVerb(EntityUid uid, MagazineAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;

        var magEnt = component.Magazine.ContainerSlot?.ContainedEntity;

        if (magEnt != null)
            RaiseLocalEvent(magEnt.Value, args);
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var ent = component.Magazine.ContainerSlot?.ContainedEntity;

        if (ent == null) return;

        // Pass the event onwards.
        RaiseLocalEvent(ent.Value, args);
        // Should be Dirtied by what other ammoprovider is handling it.

        // If no ammo then check for autoeject
        if (component.AutoEject && args.Ammo.Count == 0)
        {
            EjectMagazine(component);
            var sound = component.SoundAutoEject?.GetSound();

            if (sound != null)
                SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), sound);
        }

    }

    private void EjectMagazine(MagazineAmmoProviderComponent component)
    {
        var ent = component.Magazine.ContainerSlot?.ContainedEntity;

        if (ent == null) return;

        Slots.TryEject(component.Owner, component.Magazine, null, out _);
    }
}
