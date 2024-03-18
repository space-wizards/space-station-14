using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
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
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ComponentStartup>(OnChamberStartup);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, TakeAmmoEvent>(OnChamberMagazineTakeAmmo);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetAmmoCountEvent>(OnChamberAmmoCount);

        /*
         * Open and close bolts are separate verbs.
         * Racking does both in one hit and has a different sound (to avoid RSI + sounds cooler).
         */

        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<ActivationVerb>>(OnChamberActivationVerb);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<InteractionVerb>>(OnChamberInteractionVerb);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnMagazineVerb);

        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ActivateInWorldEvent>(OnChamberActivate);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, UseInHandEvent>(OnChamberUse);

        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, EntRemovedFromContainerMessage>(OnMagazineSlotChange);
        SubscribeLocalEvent<ChamberMagazineAmmoProviderComponent, ExaminedEvent>(OnChamberMagazineExamine);
    }

    private void OnChamberStartup(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ComponentStartup args)
    {
        // Appearance data doesn't get serialized and want to make sure this is correct on spawn (regardless of MapInit) so.
        if (component.BoltClosed != null)
        {
           Appearance.SetData(uid, AmmoVisuals.BoltClosed, component.BoltClosed.Value);
        }
    }

    /// <summary>
    /// Called when user "Activated In World" (E) with the gun as the target
    /// </summary>
    private void OnChamberActivate(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleBolt(uid, component, args.User);
    }

    /// <summary>
    /// Called when gun was "Activated In Hand" (Z)
    /// </summary>
    private void OnChamberUse(EntityUid uid, ChamberMagazineAmmoProviderComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        UseChambered(uid, component, args.User);
    }

    /// <summary>
    /// Creates "Rack" verb on the gun
    /// </summary>
    private void OnChamberActivationVerb(EntityUid uid, ChamberMagazineAmmoProviderComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.BoltClosed == null)
            return;

        args.Verbs.Add(new ActivationVerb()
        {
            Text = Loc.GetString("gun-chamber-rack"),
            Act = () =>
            {
                UseChambered(uid, component, args.User);
            }
        });
    }

    /// <summary>
    /// Opens then closes the bolt, or just closes it if currently open.
    /// </summary>
    private void UseChambered(EntityUid uid, ChamberMagazineAmmoProviderComponent component, EntityUid? user = null)
    {
        if (component.BoltClosed == false)
        {
            ToggleBolt(uid, component, user);
            return;
        }

        if (TryTakeChamberEntity(uid, out var chamberEnt))
        {
            if (_netManager.IsServer)
            {
                EjectCartridge(chamberEnt.Value);
            }
            else
            {
                // Similar to below just due to prediction.
                TransformSystem.DetachParentToNull(chamberEnt.Value, Transform(chamberEnt.Value));
            }
        }

        if (!CycleCartridge(uid, component, user))
        {
            UpdateAmmoCount(uid);
        }

        if (component.BoltClosed != false)
        {
            Audio.PlayPredicted(component.RackSound, uid, user);
        }
    }

    /// <summary>
    /// Creates "Open/Close bolt" verb on the gun
    /// </summary>
    private void OnChamberInteractionVerb(EntityUid uid, ChamberMagazineAmmoProviderComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.BoltClosed == null)
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = component.BoltClosed.Value ? Loc.GetString("gun-chamber-bolt-open") : Loc.GetString("gun-chamber-bolt-close"),
            Act = () =>
            {
                // Just toggling might be more user friendly instead of trying to set to whatever they think?
                ToggleBolt(uid, component, args.User);
            }
        });
    }

    /// <summary>
    /// Updates the bolt to its new state
    /// </summary>
    public void SetBoltClosed(EntityUid uid, ChamberMagazineAmmoProviderComponent component, bool value, EntityUid? user = null, AppearanceComponent? appearance = null, ItemSlotsComponent? slots = null)
    {
        if (component.BoltClosed == null || value == component.BoltClosed)
            return;

        Resolve(uid, ref appearance, ref slots, false);
        Appearance.SetData(uid, AmmoVisuals.BoltClosed, value, appearance);

        if (value)
        {
            CycleCartridge(uid, component, user, appearance);

            if (user != null)
                PopupSystem.PopupClient(Loc.GetString("gun-chamber-bolt-closed"), uid, user.Value);

            if (slots != null)
            {
                _slots.SetLock(uid, ChamberSlot, true, slots);
            }

            Audio.PlayPredicted(component.BoltClosedSound, uid, user);
        }
        else
        {
            if (TryTakeChamberEntity(uid, out var chambered))
            {
                if (_netManager.IsServer)
                {
                    EjectCartridge(chambered.Value);
                }
                else
                {
                    // Prediction moment
                    // The problem is client will dump the cartridge on the ground and the new server state
                    // won't correspond due to randomness so looks weird
                    // but we also need to always take it from the chamber or else ammocount won't be correct.
                    TransformSystem.DetachParentToNull(chambered.Value, Transform(chambered.Value));
                }

                UpdateAmmoCount(uid);
            }

            if (user != null)
                PopupSystem.PopupClient(Loc.GetString("gun-chamber-bolt-opened"), uid, user.Value);

            if (slots != null)
            {
                _slots.SetLock(uid, ChamberSlot, false, slots);
            }

            Audio.PlayPredicted(component.BoltOpenedSound, uid, user);
        }

        component.BoltClosed = value;
        Dirty(uid, component);
    }

    /// <summary>
    /// Tries to take ammo from the magazine and insert into the chamber.
    /// </summary>
    private bool CycleCartridge(EntityUid uid, ChamberMagazineAmmoProviderComponent component, EntityUid? user = null, AppearanceComponent? appearance = null)
    {
        // Try to put a new round in if possible.
        var magEnt = GetMagazineEntity(uid);
        var chambered = GetChamberEntity(uid);
        var result = false;

        // Similar to what takeammo does though that uses an optimised version where
        // multiple bullets may be fired in a single tick.
        if (magEnt != null && chambered == null)
        {
            var relayedArgs = new TakeAmmoEvent(1,
                new List<(EntityUid? Entity, IShootable Shootable)>(),
                Transform(uid).Coordinates,
                user);
            RaiseLocalEvent(magEnt.Value, relayedArgs);

            if (relayedArgs.Ammo.Count > 0)
            {
                var newChamberEnt = relayedArgs.Ammo[0].Entity;
                TryInsertChamber(uid, newChamberEnt!.Value);
                var ammoEv = new GetAmmoCountEvent();
                RaiseLocalEvent(magEnt.Value, ref ammoEv);
                FinaliseMagazineTakeAmmo(uid, component, ammoEv.Count, ammoEv.Capacity, user, appearance);
                UpdateAmmoCount(uid);

                // Clientside reconciliation things
                if (_netManager.IsClient)
                {
                    foreach (var (ent, _) in relayedArgs.Ammo)
                    {
                        if (!IsClientSide(ent!.Value))
                            continue;

                        Del(ent.Value);
                    }
                }
            }
            else
            {
                UpdateAmmoCount(uid);
            }

            result = true;
        }

        return result;
    }

    /// <summary>
    /// Sets the bolt's positional value to the other state
    /// </summary>
    public void ToggleBolt(EntityUid uid, ChamberMagazineAmmoProviderComponent component, EntityUid? user = null)
    {
        if (component.BoltClosed == null)
            return;

        SetBoltClosed(uid, component, !component.BoltClosed.Value, user);
    }

    /// <summary>
    /// Called when the gun was Examined
    /// </summary>
    private void OnChamberMagazineExamine(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var (count, _) = GetChamberMagazineCountCapacity(uid, component);
        string boltState;

        using (args.PushGroup(nameof(ChamberMagazineAmmoProviderComponent)))
        {
            if (component.BoltClosed != null)
            {
                if (component.BoltClosed == true)
                    boltState = Loc.GetString("gun-chamber-bolt-open-state");
                else
                    boltState = Loc.GetString("gun-chamber-bolt-closed-state");
                args.PushMarkup(Loc.GetString("gun-chamber-bolt", ("bolt", boltState),
                    ("color", component.BoltClosed.Value ? Color.FromHex("#94e1f2") : Color.FromHex("#f29d94"))));
            }

            args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", count)));
        }
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

        Containers.Remove(entity.Value, container);
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
               Containers.Insert(ammo, slot);
    }

    private void OnChamberAmmoCount(EntityUid uid, ChamberMagazineAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        OnMagazineAmmoCount(uid, component, ref args);
        args.Capacity += 1;
        var chambered = GetChamberEntity(uid);

        if (chambered != null)
        {
            args.Count += 1;
        }
    }

    private void OnChamberMagazineTakeAmmo(EntityUid uid, ChamberMagazineAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (component.BoltClosed == false)
        {
            args.Reason = Loc.GetString("gun-chamber-bolt-ammo");
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
                args.Ammo.Add((chamberEnt.Value, EnsureShootable(chamberEnt.Value)));
            }
            // No ammo returned.
            else
            {
                return;
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

                // If no more ammo then open bolt.
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

            var ammoEv = new GetAmmoCountEvent();
            RaiseLocalEvent(magEnt.Value, ref ammoEv);

            FinaliseMagazineTakeAmmo(uid, component, ammoEv.Count, ammoEv.Capacity, args.User, appearance);
        }
        // If gun doesn't autocycle (e.g. bolt-action weapons) then we leave the chambered entity in there but still return it.
        else if (Containers.TryGetContainer(uid, ChamberSlot, out var container) &&
                 container is ContainerSlot { ContainedEntity: not null } slot)
        {
            // Shooting code won't eject it if it's still contained.
            chamberEnt = slot.ContainedEntity;
            args.Ammo.Add((chamberEnt.Value, EnsureShootable(chamberEnt.Value)));
        }
    }
}
