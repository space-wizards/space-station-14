using System;
using Content.Server.Hands.Components;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void OnRangedMagMapInit(EntityUid uid, RangedMagazineComponent component, MapInitEvent args)
    {
        if (component.FillPrototype != null)
        {
            component.UnspawnedCount += component.Capacity;
        }

        UpdateRangedMagAppearance(component);
    }

    private void OnRangedMagInit(EntityUid uid, RangedMagazineComponent component, ComponentInit args)
    {
        component.AmmoContainer = uid.EnsureContainer<Container>($"{component.GetType()}-magazine", out var existing);

        if (existing)
        {
            if (component.AmmoContainer.ContainedEntities.Count > component.Capacity)
            {
                throw new InvalidOperationException("Initialized capacity of magazine higher than its actual capacity");
            }

            foreach (var entity in component.AmmoContainer.ContainedEntities)
            {
                component.SpawnedAmmo.Push(entity);
                component.UnspawnedCount--;
            }
        }

        if (TryComp(component.Owner, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
        }
    }

    private void UpdateRangedMagAppearance(RangedMagazineComponent component)
    {
        if (!TryComp(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void OnRangedMagUse(EntityUid uid, RangedMagazineComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        if (!TryComp(args.User, out HandsComponent? handsComponent))
        {
            return;
        }

        if (TakeAmmo(component) is not {Valid: true} ammo)
            return;

        var itemComponent = EntityManager.GetComponent<SharedItemComponent>(ammo);
        if (!handsComponent.CanPutInHand(itemComponent))
        {
            Transform(ammo).Coordinates = Transform(args.User).Coordinates;
            EjectCasing(ammo);
        }
        else
        {
            handsComponent.PutInHand(itemComponent);
        }

        args.Handled = true;
    }

    private void OnRangedMagExamine(EntityUid uid, RangedMagazineComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("ranged-magazine-component-on-examine", ("magazineType", component.MagazineType),("caliber", component.Caliber)));
    }

    private void OnRangedMagInteractUsing(EntityUid uid, RangedMagazineComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryInsertAmmo(args.User, args.Used, component))
            args.Handled = true;
    }

    public bool TryInsertAmmo(EntityUid user, EntityUid ammo, RangedMagazineComponent component)
    {
        if (!TryComp(ammo, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("ranged-magazine-component-try-insert-ammo-wrong-caliber"), component.Owner, Filter.Entities(user));
            return false;
        }

        if (component.ShotsLeft >= component.Capacity)
        {
            _popup.PopupEntity(Loc.GetString("ranged-magazine-component-try-insert-ammo-is-full "), component.Owner, Filter.Entities(user));
            return false;
        }

        component.AmmoContainer.Insert(ammo);
        component.SpawnedAmmo.Push(ammo);
        UpdateRangedMagAppearance(component);
        return true;
    }

    public EntityUid? TakeAmmo(RangedMagazineComponent component)
    {
        EntityUid? ammo = null;
        // If anything's spawned use that first, otherwise use the fill prototype as a fallback (if we have spawn count left)
        if (component.SpawnedAmmo.TryPop(out var entity))
        {
            ammo = entity;
            component.AmmoContainer.Remove(entity);
        }
        else if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            ammo = EntityManager.SpawnEntity(component.FillPrototype, Transform(component.Owner).Coordinates);
        }

        UpdateRangedMagAppearance(component);
        return ammo;
    }
}
