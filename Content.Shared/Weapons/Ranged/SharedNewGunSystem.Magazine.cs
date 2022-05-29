using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private const string MagazineSlot = "gun-magazine";

    protected virtual void InitializeMagazine()
    {
        SubscribeLocalEvent<MagazineAmmoProviderComponent, TakeAmmoEvent>(OnMagazineTakeAmmo);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ItemSlotChangedEvent>(OnMagazineSlotChange);
        SubscribeLocalEvent<MagazineAmmoProviderComponent, ActivateInWorldEvent>(OnMagazineActivate);
    }

    private void OnMagazineActivate(EntityUid uid, MagazineAmmoProviderComponent component, ActivateInWorldEvent args)
    {
        var ent = GetMagazineEntity(uid);

        if (ent == null) return;

        RaiseLocalEvent(ent.Value, args);
        UpdateAmmoCount(uid);
    }

    private void OnMagazineSlotChange(EntityUid uid, MagazineAmmoProviderComponent component, ref ItemSlotChangedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;
        appearance.SetData(MagazineBarrelVisuals.MagLoaded, GetMagazineEntity(uid) != null);
        UpdateAmmoCount(uid);
    }

    protected EntityUid? GetMagazineEntity(EntityUid uid)
    {
        if (!Containers.TryGetContainer(uid, MagazineSlot, out var container) ||
            container is not ContainerSlot slot) return null;
        return slot.ContainedEntity;
    }

    private void OnMagazineVerb(EntityUid uid, MagazineAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;

        var magEnt = GetMagazineEntity(uid);

        if (magEnt != null)
            RaiseLocalEvent(magEnt.Value, args);
    }

    private void OnMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var ent = GetMagazineEntity(uid);
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (ent == null)
        {
            appearance?.SetData(MagazineBarrelVisuals.MagLoaded, false);
            return;
        }

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

        // Copy the magazine's appearance data
        appearance?.SetData(MagazineBarrelVisuals.MagLoaded, true);

        if (appearance != null && TryComp<AppearanceComponent>(ent, out var magAppearance))
        {
            appearance.SetData(AmmoVisuals.AmmoCount, magAppearance.GetData<int>(AmmoVisuals.AmmoCount));
            appearance.SetData(AmmoVisuals.AmmoMax, magAppearance.GetData<int>(AmmoVisuals.AmmoMax));
        }
    }

    private void EjectMagazine(MagazineAmmoProviderComponent component)
    {
        var ent = GetMagazineEntity(component.Owner);

        if (ent == null) return;

        Slots.TryEject(component.Owner, MagazineSlot, null, out var a, excludeUserAudio: true);
    }
}
