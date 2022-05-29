using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedGunSystem
{
    protected const string MagazineSlot = "gun-magazine";

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
        UpdateAmmoCount(uid);
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;
        appearance.SetData(AmmoVisuals.MagLoaded, GetMagazineEntity(uid) != null);
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
        var magEntity = GetMagazineEntity(uid);
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (magEntity == null)
        {
            appearance?.SetData(AmmoVisuals.MagLoaded, false);
            return;
        }

        // Pass the event onwards.
        RaiseLocalEvent(magEntity.Value, args);
        // Should be Dirtied by what other ammoprovider is handling it.
        var count = 0;
        var capacity = 0;

        if (TryComp<AppearanceComponent>(magEntity, out var magAppearance))
        {
            count = magAppearance.GetData<int>(AmmoVisuals.AmmoCount);
            capacity = magAppearance.GetData<int>(AmmoVisuals.AmmoMax);
        }

        FinaliseMagazineTakeAmmo(uid, component, args, count, capacity, appearance);
    }

    private void FinaliseMagazineTakeAmmo(EntityUid uid, MagazineAmmoProviderComponent component, TakeAmmoEvent args, int count, int capacity, AppearanceComponent? appearance)
    {
        // If no ammo then check for autoeject
        if (component.AutoEject && args.Ammo.Count == 0)
        {
            EjectMagazine(component);
            var sound = component.SoundAutoEject?.GetSound();

            if (sound != null)
                SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), sound);
        }

        // Copy the magazine's appearance data
        appearance?.SetData(AmmoVisuals.MagLoaded, true);

        appearance?.SetData(AmmoVisuals.AmmoCount, count);
        appearance?.SetData(AmmoVisuals.AmmoMax, capacity);
    }

    private void EjectMagazine(MagazineAmmoProviderComponent component)
    {
        var ent = GetMagazineEntity(component.Owner);

        if (ent == null) return;

        Slots.TryEject(component.Owner, MagazineSlot, null, out var a, excludeUserAudio: true);
    }
}
