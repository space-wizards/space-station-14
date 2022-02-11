using System;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Barrels.Components;
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
    private void OnRevolverUse(EntityUid uid, RevolverBarrelComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        EjectAllSlots(component);
        component.Dirty(EntityManager);
        UpdateRevolverAppearance(component);
        args.Handled = true;
    }

    private void OnRevolverInteractUsing(EntityUid uid, RevolverBarrelComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (TryInsertBullet(args.User, args.Used, component))
            args.Handled = true;
    }

    public bool TryInsertBullet(EntityUid user, EntityUid entity, RevolverBarrelComponent component)
    {
        if (!TryComp(entity, out AmmoComponent? ammoComponent))
        {
            return false;
        }

        if (ammoComponent.Caliber != component.Caliber)
        {
            _popup.PopupEntity(Loc.GetString("revolver-barrel-component-try-insert-bullet-wrong-caliber"), component.Owner, Filter.Entities(user));
            return false;
        }

        // Functions like a stack
        // These are inserted in reverse order but then when fired Cycle will go through in order
        // The reason we don't just use an actual stack is because spin can select a random slot to point at
        for (var i = component.AmmoSlots.Length - 1; i >= 0; i--)
        {
            var slot = component.AmmoSlots[i];
            if (slot == default)
            {
                component.CurrentSlot = i;
                component.AmmoSlots[i] = entity;
                component.AmmoContainer.Insert(entity);
                SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundInsert.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));

                component.Dirty(EntityManager);
                UpdateRevolverAppearance(component);
                return true;
            }
        }

        _popup.PopupEntity(Loc.GetString("revolver-barrel-component-try-insert-bullet-ammo-full"), ammoComponent.Owner, Filter.Entities(user));
        return false;
    }

    /// <summary>
    /// Russian Roulette
    /// </summary>
    public void SpinRevolver(RevolverBarrelComponent component)
    {
        var random = _random.Next(component.AmmoSlots.Length - 1);
        component.CurrentSlot = random;
        SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundSpin.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2));
        component.Dirty(EntityManager);
    }

    public void CycleRevolver(RevolverBarrelComponent component)
    {
        // Move up a slot
        component.CurrentSlot = (component.CurrentSlot + 1) % component.AmmoSlots.Length;
        component.Dirty(EntityManager);
        UpdateRevolverAppearance(component);
    }

    private void EjectAllSlots(RevolverBarrelComponent component)
    {
        for (var i = 0; i < component.AmmoSlots.Length; i++)
        {
            var entity = component.AmmoSlots[i];
            if (entity == null) continue;

            component.AmmoContainer.Remove(entity.Value);
            EjectCasing(entity.Value);
            component.AmmoSlots[i] = null;
        }

        if (component.AmmoContainer.ContainedEntities.Count > 0)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.SoundEject.GetSound(), component.Owner, AudioParams.Default.WithVolume(-1));
        }

        // May as well point back at the end?
        component.CurrentSlot = component.AmmoSlots.Length - 1;
    }

    private void OnRevolverGetState(EntityUid uid, RevolverBarrelComponent component, ref ComponentGetState args)
    {
        var slotsSpent = new bool?[component.Capacity];
        for (var i = 0; i < component.Capacity; i++)
        {
            slotsSpent[i] = null;
            var ammoEntity = component.AmmoSlots[i];
            if (ammoEntity != default && TryComp(ammoEntity, out AmmoComponent? ammo))
            {
                slotsSpent[i] = ammo.Spent;
            }
        }

        //TODO: make yaml var to not sent currentSlot/UI? (for russian roulette)
        args.State = new RevolverBarrelComponentState(
            component.CurrentSlot,
            component.FireRateSelector,
            slotsSpent,
            component.SoundGunshot.GetSound());
    }

    private void OnRevolverMapInit(EntityUid uid, RevolverBarrelComponent component, MapInitEvent args)
    {
        component.UnspawnedCount = component.Capacity;
        var idx = 0;
        component.AmmoContainer = component.Owner.EnsureContainer<Container>($"{component.GetType()}-ammoContainer", out var existing);
        if (existing)
        {
            foreach (var entity in component.AmmoContainer.ContainedEntities)
            {
                component.UnspawnedCount--;
                component.AmmoSlots[idx] = entity;
                idx++;
            }
        }

        // TODO: Revolvers should also defer spawning T B H
        var xform = EntityManager.GetComponent<TransformComponent>(uid);

        for (var i = 0; i < component.UnspawnedCount; i++)
        {
            var entity = EntityManager.SpawnEntity(component.FillPrototype, xform.Coordinates);
            component.AmmoSlots[idx] = entity;
            component.AmmoContainer.Insert(entity);
            idx++;
        }

        UpdateRevolverAppearance(component);
        component.Dirty(EntityManager);
    }

    private void UpdateRevolverAppearance(RevolverBarrelComponent component)
    {
        if (!TryComp(component.Owner, out AppearanceComponent? appearance))
        {
            return;
        }

        // Placeholder, at this stage it's just here for the RPG
        appearance.SetData(MagazineBarrelVisuals.MagLoaded, component.ShotsLeft > 0);
        appearance.SetData(AmmoVisuals.AmmoCount, component.ShotsLeft);
        appearance.SetData(AmmoVisuals.AmmoMax, component.Capacity);
    }

    private void AddSpinVerb(EntityUid uid, RevolverBarrelComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        if (component.Capacity <= 1 || component.ShotsLeft == 0)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("spin-revolver-verb-get-data-text"),
            IconTexture = "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png",
            Act = () =>
            {
                SpinRevolver(component);
                component.Owner.PopupMessage(args.User, Loc.GetString("spin-revolver-verb-on-activate"));
            }
        };
        args.Verbs.Add(verb);
    }

    public EntityUid? PeekAmmo(RevolverBarrelComponent component)
    {
        return component.AmmoSlots[component.CurrentSlot];
    }

    /// <summary>
    /// Takes a projectile out if possible
    /// IEnumerable just to make supporting shotguns saner
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public EntityUid? TakeProjectile(RevolverBarrelComponent component, EntityCoordinates spawnAt)
    {
        var ammo = component.AmmoSlots[component.CurrentSlot];
        EntityUid? bullet = null;
        if (ammo != null)
        {
            var ammoComponent = EntityManager.GetComponent<AmmoComponent>(ammo.Value);
            bullet = TakeBullet(ammoComponent, spawnAt);
            if (ammoComponent.Caseless)
            {
                component.AmmoSlots[component.CurrentSlot] = null;
                component.AmmoContainer.Remove(ammo.Value);
            }
        }
        CycleRevolver(component);
        UpdateRevolverAppearance(component);
        return bullet;
    }
}
