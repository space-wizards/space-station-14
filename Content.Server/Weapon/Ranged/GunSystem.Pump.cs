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

    private void OnPumpGetState(EntityUid uid, PumpBarrelComponent component, ref ComponentGetState args)
    {
        (int, int)? count = (component.ShotsLeft, component.Capacity);
        var chamberedExists = component.ChamberContainer.ContainedEntity != null;
        // (Is one chambered?, is the bullet spend)
        var chamber = (chamberedExists, false);

        if (chamberedExists && TryComp<AmmoComponent?>(component.ChamberContainer.ContainedEntity!.Value, out var ammo))
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
        if (component.FillPrototype != null)
        {
            component.UnspawnedCount += component.Capacity - 1;
        }

        UpdatePumpAppearance(component);
    }

    private void UpdatePumpAppearance(PumpBarrelComponent component)
    {
        if (!TryComp(component.Owner, out AppearanceComponent? appearanceComponent)) return;

        appearanceComponent.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearanceComponent.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void OnPumpInit(EntityUid uid, PumpBarrelComponent component, ComponentInit args)
    {
        component.AmmoContainer =
            uid.EnsureContainer<Container>($"{component.GetType()}-ammo-container", out var existing);

        if (existing)
        {
            foreach (var entity in component.AmmoContainer.ContainedEntities)
            {
                component.SpawnedAmmo.Push(entity);
                component.UnspawnedCount--;
            }
        }

        component.ChamberContainer =
            uid.EnsureContainer<ContainerSlot>($"{component.GetType()}-chamber-container", out existing);

        if (existing)
        {
            component.UnspawnedCount--;
        }

        if (TryComp(uid, out AppearanceComponent? appearanceComponent))
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

    public bool TryInsertBullet(PumpBarrelComponent component, InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("pump-barrel-component-try-insert-bullet-wrong-caliber"), component.Owner, Filter.Entities(args.User));
            return false;
        }

        if (component.AmmoContainer.ContainedEntities.Count < component.Capacity - 1)
        {
            component.AmmoContainer.Insert(args.Used);
            component.SpawnedAmmo.Push(args.Used);
            component.Dirty(EntityManager);
            UpdatePumpAppearance(component);
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundInsert.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
            return true;
        }

        _popup.PopupEntity(Loc.GetString("pump-barrel-component-try-insert-bullet-no-room"), component.Owner, Filter.Entities(args.User));

        return false;
    }

    private void CyclePump(PumpBarrelComponent component, bool manual = false)
    {
        if (component.ChamberContainer.ContainedEntity is {Valid: true} chamberedEntity)
        {
            component.ChamberContainer.Remove(chamberedEntity);
            var ammoComponent = EntityManager.GetComponent<AmmoComponent>(chamberedEntity);
            if (!ammoComponent.Caseless)
            {
                EjectCasing(chamberedEntity);
            }
        }

        if (component.SpawnedAmmo.TryPop(out var next))
        {
            component.AmmoContainer.Remove(next);
            component.ChamberContainer.Insert(next);
        }

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            var ammoEntity = EntityManager.SpawnEntity(component.FillPrototype, Transform(component.Owner).Coordinates);
            component.ChamberContainer.Insert(ammoEntity);
        }

        if (manual)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundCycle.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        }

        component.Dirty(EntityManager);
        UpdatePumpAppearance(component);
    }

    public EntityUid? PeekAmmo(PumpBarrelComponent component)
    {
        return component.ChamberContainer.ContainedEntity;
    }

    public EntityUid? TakeProjectile(PumpBarrelComponent component, EntityCoordinates spawnAt)
    {
        if (!component.ManualCycle)
        {
            CyclePump(component);
        }
        else
        {
            component.Dirty(EntityManager);
        }

        if (component.ChamberContainer.ContainedEntity is not {Valid: true} chamberEntity) return null;

        var ammoComponent = EntityManager.GetComponentOrNull<AmmoComponent>(chamberEntity);

        return ammoComponent == null ? null : TakeBullet(ammoComponent, spawnAt);
    }
}
