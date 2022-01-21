using System;
using Content.Server.PowerCell;
using Content.Server.Projectiles.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Server.Weapon.Ranged;

public sealed partial class GunSystem
{
    private void OnBatteryInit(EntityUid uid, BatteryBarrelComponent component, ComponentInit args)
    {
        if (component.AmmoPrototype != null)
        {
            component.AmmoContainer = uid.EnsureContainer<ContainerSlot>($"{component.GetType()}-ammo-container");
        }

        component.Dirty(EntityManager);
    }

    private void OnBatteryMapInit(EntityUid uid, BatteryBarrelComponent component, MapInitEvent args)
    {
        UpdateBatteryAppearance(component);
    }

    private void OnBatteryGetState(EntityUid uid, BatteryBarrelComponent component, ref ComponentGetState args)
    {
        (int, int)? count = (component.ShotsLeft, component.Capacity);

        args.State = new BatteryBarrelComponentState(
            component.FireRateSelector,
            count);
    }

    private void OnCellSlotUpdated(EntityUid uid, BatteryBarrelComponent component, PowerCellChangedEvent args)
    {
        UpdateBatteryAppearance(component);
    }

    public void UpdateBatteryAppearance(BatteryBarrelComponent component)
    {
        if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, _cell.TryGetBatteryFromSlot(component.Owner, out _));
        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    public EntityUid? PeekAmmo(BatteryBarrelComponent component)
    {
        // Spawn a dummy entity because it's easier to work with I guess
        // This will get re-used for the projectile
        var ammo = component.AmmoContainer.ContainedEntity;
        if (ammo == null)
        {
            ammo = EntityManager.SpawnEntity(component.AmmoPrototype, Transform(component.Owner).Coordinates);
            component.AmmoContainer.Insert(ammo.Value);
        }

        return ammo.Value;
    }

    public EntityUid? TakeProjectile(BatteryBarrelComponent component, EntityCoordinates spawnAt)
    {
        if (!_cell.TryGetBatteryFromSlot(component.Owner, out var capacitor))
            return null;

        if (capacitor.CurrentCharge < component.LowerChargeLimit)
            return null;

        // Can fire confirmed
        // Multiply the entity's damage / whatever by the percentage of charge the shot has.
        EntityUid? entity;
        var chargeChange = Math.Min(capacitor.CurrentCharge, component.BaseFireCost);
        if (capacitor.UseCharge(chargeChange) < component.LowerChargeLimit)
        {
            // Handling of funny exploding cells.
            return null;
        }
        var energyRatio = chargeChange / component.BaseFireCost;

        if (component.AmmoContainer.ContainedEntity != null)
        {
            entity = component.AmmoContainer.ContainedEntity;
            component.AmmoContainer.Remove(entity.Value);
            Transform(entity.Value).Coordinates = spawnAt;
        }
        else
        {
            entity = EntityManager.SpawnEntity(component.AmmoPrototype, spawnAt);
        }

        if (TryComp(entity.Value, out ProjectileComponent? projectileComponent))
        {
            if (energyRatio < 1.0)
            {
                projectileComponent.Damage *= energyRatio;
            }
        }
        else if (TryComp(entity.Value, out HitscanComponent? hitscanComponent))
        {
            hitscanComponent.Damage *= energyRatio;
            hitscanComponent.ColorModifier = energyRatio;
        }
        else
        {
            throw new InvalidOperationException("Ammo doesn't have hitscan or projectile?");
        }

        // capacitor.UseCharge() triggers a PowerCellChangedEvent which will cause appearance to be updated.
        // So let's not double-call UpdateAppearance() here.
        return entity.Value;
    }
}
