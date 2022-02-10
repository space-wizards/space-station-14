using System;
using System.Collections.Generic;
using Content.Server.Hands.Components;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void AddEjectMagazineVerb(EntityUid uid, MagazineBarrelComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null ||
            !args.CanAccess ||
            !args.CanInteract ||
            !component.HasMagazine ||
            !_blocker.CanPickup(args.User))
            return;

        if (component.MagNeedsOpenBolt && !component.BoltOpen)
            return;

        AlternativeVerb verb = new()
        {
            Text = MetaData(component.MagazineContainer.ContainedEntity!.Value).EntityName,
            Category = VerbCategory.Eject,
            Act = () => RemoveMagazine(args.User, component)
        };
        args.Verbs.Add(verb);
    }

    private void AddMagazineInteractionVerbs(EntityUid uid, MagazineBarrelComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null ||
            !args.CanAccess ||
            !args.CanInteract)
            return;

        // Toggle bolt verb
        InteractionVerb toggleBolt = new()
        {
            Text = component.BoltOpen
                ? Loc.GetString("close-bolt-verb-get-data-text")
                : Loc.GetString("open-bolt-verb-get-data-text"),
            Act = () => component.BoltOpen = !component.BoltOpen
        };
        args.Verbs.Add(toggleBolt);

        // Are we holding a mag that we can insert?
        if (args.Using is not {Valid: true} @using ||
            !CanInsertMagazine(args.User, @using, component) ||
            !_blocker.CanDrop(args.User))
            return;

        // Insert mag verb
        InteractionVerb insert = new()
        {
            Text = MetaData(@using).EntityName,
            Category = VerbCategory.Insert,
            Act = () => InsertMagazine(args.User, @using, component)
        };
        args.Verbs.Add(insert);
    }

    private void OnMagazineExamine(EntityUid uid, MagazineBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine", ("caliber", component.Caliber)));

        foreach (var magazineType in GetMagazineTypes(component))
        {
            args.PushMarkup(Loc.GetString("server-magazine-barrel-component-on-examine-magazine-type", ("magazineType", magazineType)));
        }
    }

    private void OnMagazineUse(EntityUid uid, MagazineBarrelComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        // Behavior:
        // If bolt open just close it
        // If bolt closed then cycle
        //     If we cycle then get next round
        //         If no more round then open bolt

        args.Handled = true;

        if (component.BoltOpen)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundBoltClosed.GetSound(), component.Owner, AudioParams.Default.WithVolume(-5));
            _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-use-entity-bolt-closed"), component.Owner, Filter.Entities(args.User));
            component.BoltOpen = false;
            return;
        }

        // Could play a rack-slide specific sound here if you're so inclined (if the chamber is empty but rounds are available)

        CycleMagazine(component, true);
        return;
    }

    public void UpdateMagazineAppearance(MagazineBarrelComponent component)
    {
        if (!TryComp(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(BarrelBoltVisuals.BoltOpen, component.BoltOpen);
        appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, component.MagazineContainer.ContainedEntity != null);
        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void OnMagazineGetState(EntityUid uid, MagazineBarrelComponent component, ref ComponentGetState args)
    {
        (int, int)? count = null;
        if (component.MagazineContainer.ContainedEntity is {Valid: true} magazine &&
            TryComp(magazine, out RangedMagazineComponent? rangedMagazineComponent))
        {
            count = (rangedMagazineComponent.ShotsLeft, rangedMagazineComponent.Capacity);
        }

        args.State = new MagazineBarrelComponentState(
            component.ChamberContainer.ContainedEntity != null,
            component.FireRateSelector,
            count,
            component.SoundGunshot.GetSound());
    }

    private void OnMagazineInteractUsing(EntityUid uid, MagazineBarrelComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (CanInsertMagazine(args.User, args.Used, component, false))
        {
            InsertMagazine(args.User, args.Used, component);
            args.Handled = true;
            return;
        }

        // Insert 1 ammo
        if (TryComp(args.Used, out AmmoComponent? ammoComponent))
        {
            if (!component.BoltOpen)
            {
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-ammo-bolt-closed"), component.Owner, Filter.Entities(args.User));
                return;
            }

            if (ammoComponent.Caliber != component.Caliber)
            {
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"), component.Owner, Filter.Entities(args.User));
                return;
            }

            if (component.ChamberContainer.ContainedEntity == null)
            {
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-ammo-success"), component.Owner, Filter.Entities(args.User));
                component.ChamberContainer.Insert(args.Used);
                component.Dirty(EntityManager);
                UpdateMagazineAppearance(component);
                args.Handled = true;
                return;
            }

            _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-ammo-full"), component.Owner, Filter.Entities(args.User));
        }
    }

    private void OnMagazineInit(EntityUid uid, MagazineBarrelComponent component, ComponentInit args)
    {
        component.ChamberContainer = uid.EnsureContainer<ContainerSlot>($"{component.GetType()}-chamber");
        component.MagazineContainer = uid.EnsureContainer<ContainerSlot>($"{component.GetType()}-magazine", out var existing);

        if (!existing && component.MagFillPrototype != null)
        {
            var magEntity = EntityManager.SpawnEntity(component.MagFillPrototype, Transform(uid).Coordinates);
            component.MagazineContainer.Insert(magEntity);
        }

        // Temporary coz client doesn't know about magfill.
        component.Dirty(EntityManager);
    }

    private void OnMagazineMapInit(EntityUid uid, MagazineBarrelComponent component, MapInitEvent args)
    {
        UpdateMagazineAppearance(component);
    }

    public bool TryEjectChamber(MagazineBarrelComponent component)
    {
        if (component.ChamberContainer.ContainedEntity is {Valid: true} chamberEntity)
        {
            if (!component.ChamberContainer.Remove(chamberEntity))
            {
                return false;
            }
            var ammoComponent = EntityManager.GetComponent<AmmoComponent>(chamberEntity);
            if (!ammoComponent.Caseless)
            {
                EjectCasing(chamberEntity);
            }
            return true;
        }
        return false;
    }

    public bool TryFeedChamber(MagazineBarrelComponent component)
    {
        if (component.ChamberContainer.ContainedEntity != null)
        {
            return false;
        }

        // Try and pull a round from the magazine to replace the chamber if possible
        var magazine = component.MagazineContainer.ContainedEntity;
        var magComp = EntityManager.GetComponentOrNull<RangedMagazineComponent>(magazine);

        if (magComp == null || TakeAmmo(magComp) is not {Valid: true} nextRound)
        {
            return false;
        }

        component.ChamberContainer.Insert(nextRound);

        if (component.AutoEjectMag && magazine != null && EntityManager.GetComponent<RangedMagazineComponent>(magazine.Value).ShotsLeft == 0)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundAutoEject.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));

            component.MagazineContainer.Remove(magazine.Value);
            // TODO: Should be a state or something, waste of bandwidth
            RaiseNetworkEvent(new MagazineAutoEjectEvent {Uid = component.Owner});
        }
        return true;
    }

    private void CycleMagazine(MagazineBarrelComponent component, bool manual = false)
    {
        if (component.BoltOpen)
            return;

        TryEjectChamber(component);

        TryFeedChamber(component);

        if (component.ChamberContainer.ContainedEntity == null && !component.BoltOpen)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundBoltOpen.GetSound(), component.Owner, AudioParams.Default.WithVolume(-5));

            if (_container.TryGetContainingContainer(component.Owner, out var container))
            {
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-cycle-bolt-open"), component.Owner, Filter.Entities(container.Owner));
            }

            component.BoltOpen = true;
            return;
        }

        if (manual)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundRack.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        }

        component.Dirty(EntityManager);
        UpdateMagazineAppearance(component);
    }

    public EntityUid? PeekAmmo(MagazineBarrelComponent component)
    {
        return component.BoltOpen ? null : component.ChamberContainer.ContainedEntity;
    }

    public EntityUid? TakeProjectile(MagazineBarrelComponent component, EntityCoordinates spawnAt)
    {
        if (component.BoltOpen)
            return null;

        var entity = component.ChamberContainer.ContainedEntity;

        CycleMagazine(component);

        return entity != null ? TakeBullet(EntityManager.GetComponent<AmmoComponent>(entity.Value), spawnAt) : null;
    }

    public List<MagazineType> GetMagazineTypes(MagazineBarrelComponent component)
    {
        var types = new List<MagazineType>();

        foreach (MagazineType mag in Enum.GetValues(typeof(MagazineType)))
        {
            if ((component.MagazineTypes & mag) != 0)
            {
                types.Add(mag);
            }
        }

        return types;
    }

    public void RemoveMagazine(EntityUid user, MagazineBarrelComponent component)
    {
        var mag = component.MagazineContainer.ContainedEntity;

        if (mag == null)
            return;

        if (component.MagNeedsOpenBolt && !component.BoltOpen)
        {
            _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-remove-magazine-bolt-closed"), component.Owner, Filter.Entities(user));
            return;
        }

        component.MagazineContainer.Remove(mag.Value);
        SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundMagEject.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));

        if (TryComp(user, out HandsComponent? handsComponent))
        {
            handsComponent.PutInHandOrDrop(EntityManager.GetComponent<SharedItemComponent>(mag.Value));
        }

        component.Dirty(EntityManager);
        UpdateMagazineAppearance(component);
    }

    public bool CanInsertMagazine(EntityUid user, EntityUid magazine, MagazineBarrelComponent component, bool quiet = true)
    {
        if (!TryComp(magazine, out RangedMagazineComponent? magazineComponent))
        {
            return false;
        }

        if ((component.MagazineTypes & magazineComponent.MagazineType) == 0)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-wrong-magazine-type"), component.Owner, Filter.Entities(user));

            return false;
        }

        if (magazineComponent.Caliber != component.Caliber)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"), component.Owner, Filter.Entities(user));

            return false;
        }

        if (component.MagNeedsOpenBolt && !component.BoltOpen)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-bolt-closed"), component.Owner, Filter.Entities(user));

            return false;
        }

        if (component.MagazineContainer.ContainedEntity == null)
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-already-holding-magazine"), component.Owner, Filter.Entities(user));

        return false;
    }

    public void InsertMagazine(EntityUid user, EntityUid magazine, MagazineBarrelComponent component)
    {
        SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundMagInsert.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        _popup.PopupEntity(Loc.GetString("server-magazine-barrel-component-interact-using-success"), component.Owner, Filter.Entities(user));
        component.MagazineContainer.Insert(magazine);
        component.Dirty(EntityManager);
        UpdateMagazineAppearance(component);
    }
}
