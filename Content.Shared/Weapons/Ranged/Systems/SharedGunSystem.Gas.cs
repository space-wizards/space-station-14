using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected const string TankSlot = "gun_tank";

    protected virtual void InitializeGas()
    {
        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentInit>(OnGasInit);
        SubscribeLocalEvent<GasAmmoProviderComponent, TakeAmmoEvent>(OnGasTakeAmmo);
        SubscribeLocalEvent<GasAmmoProviderComponent, GetAmmoCountEvent>(OnGasAmmoCount);
        SubscribeLocalEvent<GasAmmoProviderComponent, ItemSlotChangedEvent>(OnTankSlotChanged);

        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentGetState>(OnGasGetState);
        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentHandleState>(OnGasHandleState);
    }
    private void OnGasGetState(EntityUid uid, GasAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new GasAmmoProviderComponentState(component.Moles, component.MolesPerShot);
    }

    private void OnGasHandleState(EntityUid uid, GasAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is GasAmmoProviderComponentState state)
        {
            component.Moles = state.Moles;
            component.MolesPerShot = state.MolesPerShot;
        }
    }

    private void OnTankSlotChanged(EntityUid uid, GasAmmoProviderComponent component, ref ItemSlotChangedEvent args)
    {
        component.TankEntity = GetTankEntity(uid);
        UpdateGas(uid, component, false);
        UpdateGasAppearance(component);
    }

    private void OnGasInit(EntityUid uid, GasAmmoProviderComponent component, ComponentInit args)
    {
        component.TankEntity = GetTankEntity(uid);
        UpdateGasAppearance(component);
    }

    private void OnGasTakeAmmo(EntityUid uid, GasAmmoProviderComponent component, TakeAmmoEvent args)
    {
        component.TankEntity = GetTankEntity(uid);
        if (component.TankEntity == null)
            return;

        for (var i = 0; i < args.Shots; i++)
        {
            if (component.Moles < component.MolesPerShot)
                return;

            component.Moles -= component.MolesPerShot;

            var ent = Spawn(component.Proto, args.Coordinates);
            args.Ammo.Add(EnsureComp<AmmoComponent>(ent));
        }

        UpdateGas(uid, component, true);
        UpdateGasAppearance(component);
        Dirty(component);
    }

    protected virtual void UpdateGas(EntityUid uid, GasAmmoProviderComponent component, bool shotFired) { }

    protected EntityUid? GetTankEntity(EntityUid uid)
    {
        if (!Containers.TryGetContainer(uid, TankSlot, out var container) ||
            container is not ContainerSlot slot) return null;
        return slot.ContainedEntity;
    }

    private void OnGasAmmoCount(EntityUid uid, GasAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Capacity = 1; //figure out what to do with this. tanks don't have hard capacity.
        args.Count = (int) (component.Moles / component.MolesPerShot);
    }

    protected void UpdateGasAppearance(GasAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        appearance.SetData(AmmoVisuals.MagLoaded, component.TankEntity != null);
        appearance.SetData(AmmoVisuals.HasAmmo, component.Moles >= component.MolesPerShot);
        appearance.SetData(AmmoVisuals.AmmoCount, component.Moles/component.MolesPerShot);
    }
}
