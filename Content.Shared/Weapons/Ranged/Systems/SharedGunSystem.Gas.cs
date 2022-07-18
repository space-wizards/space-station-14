using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeGas()
    {
        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentInit>(OnGasInit);
        SubscribeLocalEvent<GasAmmoProviderComponent, TakeAmmoEvent>(OnGasTakeAmmo);
        SubscribeLocalEvent<GasAmmoProviderComponent, GetAmmoCountEvent>(OnGasAmmoCount);

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

    private void OnGasInit(EntityUid uid, GasAmmoProviderComponent component, ComponentInit args)
    {
        UpdateGasAppearance(component);
    }

    private void OnGasTakeAmmo(EntityUid uid, GasAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (int i = 0; i < args.Shots; i++)
        {
            if (component.Moles < component.MolesPerShot)
                return;

            component.Moles -= component.MolesPerShot;

            var ent = Spawn(component.Proto, args.Coordinates);
            args.Ammo.Add(EnsureComp<AmmoComponent>(ent));
        }

        TakeGas(uid, component);
        UpdateGasAppearance(component);
        Dirty(component);
    }

    protected virtual void TakeGas(EntityUid uid, GasAmmoProviderComponent component) { }

    private void OnGasAmmoCount(EntityUid uid, GasAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Capacity = 1; //figure out what to do with this. tanks don't have hard capacity.
        args.Count = (int) (component.Moles / component.MolesPerShot);
    }

    protected void UpdateGasAppearance(GasAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(component.Owner, out var appearance)) return;
            appearance.SetData(AmmoVisuals.HasAmmo, component.Moles >= component.MolesPerShot);
            appearance.SetData(AmmoVisuals.AmmoCount, component.Moles/component.MolesPerShot);
    }
}
