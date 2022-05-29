using System.Diagnostics.CodeAnalysis;
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
    private const string ChamberSlot = "gun-chamber";

    private void InitializeChamberMagazine()
    {
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, TakeAmmoEvent>(OnChamberMagazineTakeAmmo);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ItemSlotChangedEvent>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ActivateInWorldEvent>(OnMagazineActivate);
    }

    protected bool TryGetChamberEntity(EntityUid uid, [NotNullWhen(true)] out EntityUid? entity, [NotNullWhen(true)] out IContainer? container)
    {
        if (!Containers.TryGetContainer(uid, ChamberSlot, out container) ||
            container is not ContainerSlot slot)
        {
            entity = null;
            return false;
        }

        entity = slot.ContainedEntity;
        return entity != null;
    }

    private void OnChamberMagazineTakeAmmo(EntityUid uid, ChamberMagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        // So chamber logic is kinda sussier than the others
        // Essentially we want to treat the chamber as a potentially free slot and then the mag as the remaining slots
        // i.e. if we shoot 3 times, then we use the chamber once (regardless if it's empty or not) and 2 from the mag
        // We move the n + 1 shot into the chamber as we essentially treat it like a stack.

        for (var i = 0; i < args.Shots; i++)
        {
            if (i == 0)
            {

            }
        }

        // Move the nth shot into the chamber.

        if (TryGetChamberEntity(uid, out var chamberEnt, out var container))
        {
            args.Ammo.Add(EnsureComp<NewAmmoComponent>(chamberEnt.Value));
            container.Remove(chamberEnt.Value);
            return;
        }

        var ent = GetMagazineEntity(uid);
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (ent == null)
        {
            appearance?.SetData(MagazineBarrelVisuals.MagLoaded, false);
            return;
        }

        // TODO: This is fucking copy-pasted tell sloth to to do something about
        // Pass the event onwards.
        RaiseLocalEvent(ent.Value, args);
        // Should be Dirtied by what other ammoprovider is handling it.

        // So if we shoot 1 bullet we need to pull one into the chamber
        // If we shoot 2 then we shoot chamber + 1 from mag and pull another into chamber
        // i.e. shoot count + 1 goes into the chamber.
        if (args.Ammo.Count > 0)
        {
            // Shoot all but the last one?
            if (Containers.TryGetContainer(uid, ChamberSlot, out var chamberContainer))
            {
                chamberContainer.
            }
        }

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
}
