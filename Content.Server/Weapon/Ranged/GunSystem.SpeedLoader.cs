using Content.Server.Hands.Components;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
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
    private void OnSpeedLoaderInit(EntityUid uid, SpeedLoaderComponent component, ComponentInit args)
    {
        component.AmmoContainer = uid.EnsureContainer<Container>($"{component.GetType()}-container", out var existing);

        if (existing)
        {
            foreach (var ammo in component.AmmoContainer.ContainedEntities)
            {
                component.UnspawnedCount--;
                component.SpawnedAmmo.Push(ammo);
            }
        }
    }

    private void OnSpeedLoaderMapInit(EntityUid uid, SpeedLoaderComponent component, MapInitEvent args)
    {
        component.UnspawnedCount += component.Capacity;
        UpdateSpeedLoaderAppearance(component);
    }

    private void UpdateSpeedLoaderAppearance(SpeedLoaderComponent component)
    {
        if (!TryComp(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.AmmoLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private EntityUid? TakeAmmo(SpeedLoaderComponent component)
    {
        if (component.SpawnedAmmo.TryPop(out var entity))
        {
            component.AmmoContainer.Remove(entity);
            return entity;
        }

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            return EntityManager.SpawnEntity(component.FillPrototype, Transform(component.Owner).Coordinates);
        }

        return null;
    }

    private void OnSpeedLoaderUse(EntityUid uid, SpeedLoaderComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        if (!TryComp(args.User, out HandsComponent? handsComponent))
        {
            return;
        }

        var ammo = TakeAmmo(component);
        if (ammo == null)
        {
            return;
        }

        var itemComponent = EntityManager.GetComponent<SharedItemComponent>(ammo.Value);
        if (!handsComponent.CanPutInHand(itemComponent))
        {
            EjectCasing(ammo.Value);
        }
        else
        {
            handsComponent.PutInHand(itemComponent);
        }

        UpdateSpeedLoaderAppearance(component);
        args.Handled = true;
    }

    private void OnSpeedLoaderAfterInteract(EntityUid uid, SpeedLoaderComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach) return;

        if (args.Target == null)
        {
            return;
        }

        // This area is dirty but not sure of an easier way to do it besides add an interface or somethin
        var changed = false;

        if (TryComp(args.Target.Value, out RevolverBarrelComponent? revolverBarrel))
        {
            for (var i = 0; i < component.Capacity; i++)
            {
                var ammo = TakeAmmo(component);
                if (ammo == null)
                {
                    break;
                }

                if (TryInsertBullet(args.User, ammo.Value, revolverBarrel))
                {
                    changed = true;
                    continue;
                }

                // Take the ammo back
                TryInsertAmmo(args.User, ammo.Value, component);
                break;
            }
        }
        else if (TryComp(args.Target.Value, out BoltActionBarrelComponent? boltActionBarrel))
        {
            for (var i = 0; i < component.Capacity; i++)
            {
                var ammo = TakeAmmo(component);
                if (ammo == null)
                {
                    break;
                }

                if (TryInsertBullet(args.User, ammo.Value, boltActionBarrel))
                {
                    changed = true;
                    continue;
                }

                // Take the ammo back
                TryInsertAmmo(args.User, ammo.Value, component);
                break;
            }

        }

        if (changed)
        {
            UpdateSpeedLoaderAppearance(component);
        }

        args.Handled = true;
    }

    public bool TryInsertAmmo(EntityUid user, EntityUid entity, SpeedLoaderComponent component)
    {
        if (!TryComp(entity, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("speed-loader-component-try-insert-ammo-wrong-caliber"), component.Owner, Filter.Entities(user));
            return false;
        }

        if (component.AmmoLeft >= component.Capacity)
        {
            _popup.PopupEntity(Loc.GetString("speed-loader-component-try-insert-ammo-no-room"), component.Owner, Filter.Entities(user));
            return false;
        }

        component.SpawnedAmmo.Push(entity);
        component.AmmoContainer.Insert(entity);
        UpdateSpeedLoaderAppearance(component);
        return true;

    }

    private void OnSpeedLoaderInteractUsing(EntityUid uid, SpeedLoaderComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryInsertAmmo(args.User, args.Used, component))
            args.Handled = true;
    }
}
