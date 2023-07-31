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
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnMagazineVerb);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntRemovedFromContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UseInHandEvent>(OnChamberUse);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ExaminedEvent>(OnChamberMagazineExamine);
    }

    private void OnChamberUse(EntityUid uid, ChamberMagazineAmmoProviderComponent component, UseInHandEvent args)
    {
        // (Chamber but no bolt is whacky) so just relay it.
        if (component.BoltClosed == null)
        {
            OnMagazineUse(uid, component, args);
            return;
        }

        ToggleBolt(uid, component);
    }

    public void SetBoltClosed(EntityUid uid, ChamberMagazineAmmoProviderComponent component, bool value, EntityUid? user = null, AppearanceComponent? appearance = null)
    {
        if (component.BoltClosed == null || value == component.BoltClosed)
            return;

        Resolve(uid, ref appearance, false);
        Appearance.SetData(uid, AmmoVisuals.BoltClosed, component.BoltClosed.Value, appearance);

        if (value)
        {
            Audio.PlayPredicted(component.BoltClosedSound, uid, user);
        }
        else
        {
            if (TryTakeChamberEntity(uid, out var chambered))
            {
                EjectCartridge(chambered.Value);
            }

            Audio.PlayPredicted(component.BoltOpenSound, uid, user);
        }

        component.BoltClosed = value;
        Dirty(uid, component);

        // Toggling bolt on a gun that autocycles doesn't do anything.
        // However setting it on a gun that doesn't autocycle.
    }

    public void ToggleBolt(EntityUid uid, ChamberMagazineAmmoProviderComponent component)
    {
        if (component.BoltClosed == null)
            return;

        SetBoltClosed(uid, component, !component.BoltClosed.Value);
    }

    private void OnChamberMagazineExamine(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (count, _) = GetChamberMagazineCountCapacity(uid, component);

        if (component.BoltClosed != null)
        {
            args.PushMarkup(Loc.GetString("gun-chamber-bolt", ("bolt", component.BoltClosed), ("color", component.BoltClosed.Value ? Color.FromHex("#94e1f2") : Color.FromHex("#f29d94"))));
        }

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
        if (entity == null)
            return false;

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

    protected (int, int) GetChamberMagazineCountCapacity(EntityUid uid, ChamberMagazineAmmoProviderComponent component)
    {
        var count = GetChamberEntity(uid) != null ? 1 : 0;
        var (magCount, magCapacity) = GetMagazineCountCapacity(uid, component);
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
        if (component.BoltClosed == false)
        {
            return;
        }

        // So chamber logic is kinda sussier than the others
        // Essentially we want to treat the chamber as a potentially free slot and then the mag as the remaining slots
        // i.e. if we shoot 3 times, then we use the chamber once (regardless if it's empty or not) and 2 from the mag
        // We move the n + 1 shot into the chamber as we essentially treat it like a stack.
        TryComp<AppearanceComponent>(uid, out var appearance);

        EntityUid? chamberEnt;

        // Normal behaviour for guns.
        if (component.AutoCycle)
        {
            if (TryTakeChamberEntity(uid, out chamberEnt))
            {
                args.Ammo.Add((chamberEnt.Value, EnsureComp<AmmoComponent>(chamberEnt.Value)));
            }

            var magEnt = GetMagazineEntity(uid);

            // Pass an event to the magazine to get more (to refill chamber or for shooting).
            if (magEnt != null)
            {
                // We pass in Shots not Shots - 1 as we'll take the last entity and move it into the chamber.
                var relayedArgs = new TakeAmmoEvent(args.Shots, new List<(EntityUid? Entity, IShootable Shootable)>(), args.Coordinates, args.User);
                RaiseLocalEvent(magEnt.Value, relayedArgs);

                // Put in the nth slot back into the chamber
                // Rest of the ammo gets shot
                if (relayedArgs.Ammo.Count > 0)
                {
                    var newChamberEnt = relayedArgs.Ammo[^1].Entity;
                    TryInsertChamber(uid, newChamberEnt!.Value);
                }

                // Anything above the chamber-refill amount gets fired.
                for (var i = 0; i < relayedArgs.Ammo.Count - 1; i++)
                {
                    args.Ammo.Add(relayedArgs.Ammo[i]);
                }

                // TODO: If no more ammo then open bolt.
                if (relayedArgs.Ammo.Count == 0)
                {
                    SetBoltClosed(uid, component, false, user: args.User, appearance: appearance);
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
            RaiseLocalEvent(magEnt.Value, ref ammoEv);

            FinaliseMagazineTakeAmmo(uid, component, args, count + ammoEv.Count, capacity + ammoEv.Capacity, appearance);
        }
        // If gun doesn't autocycle (e.g. bolt-action weapons) then we leave the chambered entity in there but still return it.
        else if (Containers.TryGetContainer(uid, ChamberSlot, out var container) &&
                 container is ContainerSlot { ContainedEntity: not null } slot)
        {
            // Shooting code won't eject it if it's still contained.
            chamberEnt = slot.ContainedEntity;
            args.Ammo.Add((chamberEnt.Value, EnsureComp<AmmoComponent>(chamberEnt.Value)));
        }
    }
}
