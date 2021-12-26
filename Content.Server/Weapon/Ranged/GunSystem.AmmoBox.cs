using System;
using System.Collections.Generic;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    // Probably needs combining with magazines in future given the common functionality.

    private void OnAmmoBoxAltVerbs(EntityUid uid, AmmoBoxComponent component, GetAlternativeVerbsEvent args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (component.AmmoLeft == 0)
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("dump-vert-get-data-text"),
            IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png",
            Act = () => AmmoBoxEjectContents(component, 10)
        };
        args.Verbs.Add(verb);
    }

    private void OnAmmoBoxInteractHand(EntityUid uid, AmmoBoxComponent component, InteractHandEvent args)
    {
        if (args.Handled) return;

        TryUse(args.User, component);
    }

    private void OnAmmoBoxUse(EntityUid uid, AmmoBoxComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        TryUse(args.User, component);
    }

    private void OnAmmoBoxInteractUsing(EntityUid uid, AmmoBoxComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (EntityManager.TryGetComponent(args.Used, out AmmoComponent? ammoComponent))
        {
            if (TryInsertAmmo(args.User, args.Used, component, ammoComponent))
            {
                args.Handled = true;
            }

            return;
        }

        if (!EntityManager.TryGetComponent(args.Used, out RangedMagazineComponent? rangedMagazine)) return;

        for (var i = 0; i < Math.Max(10, rangedMagazine.ShotsLeft); i++)
        {
            if (rangedMagazine.TakeAmmo() is not {Valid: true} ammo)
            {
                continue;
            }

            if (!TryInsertAmmo(args.User, ammo, component))
            {
                rangedMagazine.TryInsertAmmo(args.User, ammo);
                args.Handled = true;
                return;
            }
        }

        args.Handled = true;
    }

    private void OnAmmoBoxInit(EntityUid uid, AmmoBoxComponent component, ComponentInit args)
    {
        component.AmmoContainer = uid.EnsureContainer<Container>($"{component.Name}-container", out var existing);

        if (existing)
        {
            foreach (var entity in component.AmmoContainer.ContainedEntities)
            {
                component.UnspawnedCount--;
                component.SpawnedAmmo.Push(entity);
                component.AmmoContainer.Insert(entity);
            }
        }
    }

    private void OnAmmoBoxExamine(EntityUid uid, AmmoBoxComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ammo-box-component-on-examine-caliber-description", ("caliber", component.Caliber)));
        args.PushMarkup(Loc.GetString("ammo-box-component-on-examine-remaining-ammo-description", ("ammoLeft", component.AmmoLeft),("capacity", component.Capacity)));
    }

    private void OnAmmoBoxMapInit(EntityUid uid, AmmoBoxComponent component, MapInitEvent args)
    {
        component.UnspawnedCount += component.Capacity;
        UpdateAmmoBoxAppearance(uid, component);
    }

    private void UpdateAmmoBoxAppearance(EntityUid uid, AmmoBoxComponent ammoBox, AppearanceComponent? appearanceComponent = null)
    {
        if (!Resolve(uid, ref appearanceComponent, false)) return;

        appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
        appearanceComponent.SetData(AmmoVisuals.AmmoCount, ammoBox.AmmoLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, ammoBox.Capacity);
    }

    private void AmmoBoxEjectContents(AmmoBoxComponent ammoBox, int count)
    {
        var ejectCount = Math.Min(count, ammoBox.Capacity);
        var ejectAmmo = new List<EntityUid>(ejectCount);

        for (var i = 0; i < Math.Min(count, ammoBox.Capacity); i++)
        {
            if (TakeAmmo(ammoBox) is not { } ammo)
            {
                break;
            }

            ejectAmmo.Add(ammo);
        }

        ServerRangedBarrelComponent.EjectCasings(ejectAmmo);
        UpdateAmmoBoxAppearance(ammoBox.Owner, ammoBox);
    }

    private bool TryUse(EntityUid user, AmmoBoxComponent ammoBox)
    {
        if (!EntityManager.TryGetComponent(user, out HandsComponent? handsComponent))
        {
            return false;
        }

        if (TakeAmmo(ammoBox) is not { } ammo)
        {
            return false;
        }

        if (EntityManager.TryGetComponent(ammo, out ItemComponent? item))
        {
            if (!handsComponent.CanPutInHand(item))
            {
                TryInsertAmmo(user, ammo, ammoBox);
                return false;
            }

            handsComponent.PutInHand(item);
        }

        UpdateAmmoBoxAppearance(ammoBox.Owner, ammoBox);
        return true;
    }

    public bool TryInsertAmmo(EntityUid user, EntityUid ammo, AmmoBoxComponent ammoBox, AmmoComponent? ammoComponent = null)
    {
        if (!Resolve(ammo, ref ammoComponent, false))
        {
            return false;
        }

        if (ammoComponent.Caliber != ammoBox.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("ammo-box-component-try-insert-ammo-wrong-caliber"), ammo, Filter.Entities(user));
            return false;
        }

        if (ammoBox.AmmoLeft >= ammoBox.Capacity)
        {
            _popup.PopupEntity(Loc.GetString("ammo-box-component-try-insert-ammo-no-room"), ammo, Filter.Entities(user));
            return false;
        }

        ammoBox.SpawnedAmmo.Push(ammo);
        ammoBox.AmmoContainer.Insert(ammo);
        UpdateAmmoBoxAppearance(ammoBox.Owner, ammoBox);
        return true;
    }

    public EntityUid? TakeAmmo(AmmoBoxComponent ammoBox, TransformComponent? xform = null)
    {
        if (!Resolve(ammoBox.Owner, ref xform)) return null;

        if (ammoBox.SpawnedAmmo.TryPop(out var ammo))
        {
            ammoBox.AmmoContainer.Remove(ammo);
            return ammo;
        }

        if (ammoBox.UnspawnedCount > 0)
        {
            ammo = EntityManager.SpawnEntity(ammoBox.FillPrototype, xform.Coordinates);

            // when dumping from held ammo box, this detaches the spawned ammo from the player.
            EntityManager.GetComponent<TransformComponent>(ammo).AttachParentToContainerOrGrid();

            ammoBox.UnspawnedCount--;
        }

        return ammo;
    }
}
