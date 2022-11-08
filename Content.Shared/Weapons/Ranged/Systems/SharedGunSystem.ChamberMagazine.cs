using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected const string ChamberSlot = "gun_chamber";

    protected virtual void InitializeChamberMagazine()
    {
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, TakeAmmoEvent>(OnChamberMagazineTakeAmmo);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<Verb>>(OnMagazineVerb);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntRemovedFromContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UseInHandEvent>(OnMagazineUse);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ExaminedEvent>(OnChamberMagazineExamine);
    }

    private void OnChamberMagazineExamine(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (count, _) = GetChamberMagazineCountCapacity(component);
        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", count)));
    }

    private bool TryTakeChamberEntity(EntityUid uid, [NotNullWhen(true)] out EntityUid? entity)
    {
        if (!Containers.TryGetContainer(uid, ChamberSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            entity = null;
            return false;
        }

        entity = slot.ContainedEntity;
        if (entity == null) return false;
        container.Remove(entity.Value);
        return true;
    }

    protected EntityUid? GetChamberEntity(EntityUid uid)
    {
        if (!Containers.TryGetContainer(uid, ChamberSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            return null;
        }

        return slot.ContainedEntity;
    }

    protected (int, int) GetChamberMagazineCountCapacity(ChamberMagazineAmmoProviderComponent component)
    {
        var count = GetChamberEntity(component.Owner) != null ? 1 : 0;
        var (magCount, magCapacity) = GetMagazineCountCapacity(component);
        return (count + magCount, magCapacity);
    }

    private bool TryInsertChamber(EntityUid uid, EntityUid ammo)
    {
        return Containers.TryGetContainer(uid, ChamberSlot, out var container) &&
               container is ContainerSlot slot &&
               slot.Insert(ammo);
    }

    private void OnChamberMagazineTakeAmmo(EntityUid uid, ChamberMagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        // So chamber logic is kinda sussier than the others
        // Essentially we want to treat the chamber as a potentially free slot and then the mag as the remaining slots
        // i.e. if we shoot 3 times, then we use the chamber once (regardless if it's empty or not) and 2 from the mag
        // We move the n + 1 shot into the chamber as we essentially treat it like a stack.
        TryComp<AppearanceComponent>(uid, out var appearance);

        if (TryTakeChamberEntity(uid, out var chamberEnt))
        {
            args.Ammo.Add(EnsureComp<AmmoComponent>(chamberEnt.Value));
        }

        var magEnt = GetMagazineEntity(uid);

        // Pass an event to the magazine to get more (to refill chamber or for shooting).
        if (magEnt != null)
        {
            // We pass in Shots not Shots - 1 as we'll take the last entity and move it into the chamber.
            var relayedArgs = new TakeAmmoEvent(args.Shots, new List<IShootable>(), args.Coordinates, args.User);
            RaiseLocalEvent(magEnt.Value, relayedArgs, false);

            // Put in the nth slot back into the chamber
            // Rest of the ammo gets shot
            if (relayedArgs.Ammo.Count > 0)
            {
                var newChamberEnt = ((AmmoComponent) relayedArgs.Ammo[^1]).Owner;
                TryInsertChamber(uid, newChamberEnt);
            }

            // Anything above the chamber-refill amount gets fired.
            for (var i = 0; i < relayedArgs.Ammo.Count - 1; i++)
            {
                args.Ammo.Add(relayedArgs.Ammo[i]);
            }
        }
        else
        {
            Appearance.SetData(uid, AmmoVisuals.MagLoaded, false, appearance);
            return;
        }

        var count = chamberEnt != null ? 1 : 0;
        const int capacity = 1;

        var ammoEv = new GetAmmoCountEvent();
        RaiseLocalEvent(magEnt.Value, ref ammoEv, false);

        FinaliseMagazineTakeAmmo(uid, component, args, count + ammoEv.Count, capacity + ammoEv.Capacity, appearance);
    }
}
