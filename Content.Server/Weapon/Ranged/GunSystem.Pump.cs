using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
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
    private void OnPumpExamine(EntityUid uid, PumpBarrelComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("pump-barrel-component-on-examine", ("caliber", component.Caliber)));
    }

    private void OnPumpGetState(EntityUid uid, PumpBarrelComponent component, ComponentGetState args)
    {
        (int, int)? count = (component.ShotsLeft, component.Capacity);
        var chamberedExists = component._chamberContainer.ContainedEntity != null;
        // (Is one chambered?, is the bullet spend)
        var chamber = (chamberedExists, false);

        if (chamberedExists && EntityManager.TryGetComponent<AmmoComponent?>(component._chamberContainer.ContainedEntity!.Value, out var ammo))
        {
            chamber.Item2 = ammo.Spent;
        }

        args.State = new PumpBarrelComponentState(
            chamber,
            component.FireRateSelector,
            count,
            component.SoundGunshot.GetSound());
    }

    private void OnPumpMapInit(EntityUid uid, PumpBarrelComponent component, MapInitEvent args)
    {
        if (component._fillPrototype != null)
        {
            component._unspawnedCount += component.Capacity - 1;
        }

        UpdatePumpAppearance(component);
    }

    private void UpdatePumpAppearance(PumpBarrelComponent component)
    {
        if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void OnPumpInit(EntityUid uid, PumpBarrelComponent component, ComponentInit args)
    {
        component._ammoContainer =
            uid.EnsureContainer<Container>($"{nameof(component)}-ammo-container", out var existing);

        if (existing)
        {
            foreach (var entity in component._ammoContainer.ContainedEntities)
            {
                component._spawnedAmmo.Push(entity);
                component._unspawnedCount--;
            }
        }

        component._chamberContainer =
            uid.EnsureContainer<ContainerSlot>($"{nameof(component)}-chamber-container", out existing);

        if (existing)
        {
            component._unspawnedCount--;
        }

        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearanceComponent))
        {
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
        }

        component.Dirty(EntityManager);
        UpdatePumpAppearance(component);
    }

    private void OnPumpUse(EntityUid uid, PumpBarrelComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        args.Handled = true;
        CyclePump(component, true);
    }

    private void OnPumpInteractUsing(EntityUid uid, PumpBarrelComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryInsertBullet(component, args))
            args.Handled = true;
    }

    public bool TryInsertBullet(PumpBarrelComponent component, InteractUsingEventArgs args)
    {
        if (!EntityManager.TryGetComponent(args.Using, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("pump-barrel-component-try-insert-bullet-wrong-caliber"), component.Owner, Filter.Entities(args.User));
            return false;
        }

        if (component._ammoContainer.ContainedEntities.Count < component.Capacity - 1)
        {
            component._ammoContainer.Insert(args.Using);
            component._spawnedAmmo.Push(args.Using);
            component.Dirty(EntityManager);
            UpdatePumpAppearance(component);
            SoundSystem.Play(Filter.Pvs(component.Owner), component._soundInsert.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
            return true;
        }

        _popup.PopupEntity(Loc.GetString("pump-barrel-component-try-insert-bullet-no-room"), component.Owner, Filter.Entities(args.User));

        return false;
    }

    private void CyclePump(PumpBarrelComponent component, bool manual = false)
    {
        if (component._chamberContainer.ContainedEntity is {Valid: true} chamberedEntity)
        {
            component._chamberContainer.Remove(chamberedEntity);
            var ammoComponent = EntityManager.GetComponent<AmmoComponent>(chamberedEntity);
            if (!ammoComponent.Caseless)
            {
                EjectCasing(chamberedEntity);
            }
        }

        if (component._spawnedAmmo.TryPop(out var next))
        {
            component._ammoContainer.Remove(next);
            component._chamberContainer.Insert(next);
        }

        if (component._unspawnedCount > 0)
        {
            component._unspawnedCount--;
            var ammoEntity = EntityManager.SpawnEntity(component._fillPrototype, Transform(component.Owner).Coordinates);
            component._chamberContainer.Insert(ammoEntity);
        }

        if (manual)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component._soundCycle.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        }

        component.Dirty(EntityManager);
        UpdatePumpAppearance(component);
    }

    public EntityUid? PeekPumpAmmo(PumpBarrelComponent component)
    {
        return component._chamberContainer.ContainedEntity;
    }

    public EntityUid? TakeProjectile(PumpBarrelComponent component, EntityCoordinates spawnAt)
    {
        if (!component._manualCycle)
        {
            CyclePump(component);
        }
        else
        {
            component.Dirty(EntityManager);
        }

        if (component._chamberContainer.ContainedEntity is not {Valid: true} chamberEntity) return null;

        var ammoComponent = EntityManager.GetComponentOrNull<AmmoComponent>(chamberEntity);

        return ammoComponent == null ? null : TakeBullet(ammoComponent, spawnAt);
    }
}
