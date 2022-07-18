using Content.Server.Atmos.Components;
using Content.Server.Power.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeGas()
    {
        base.InitializeGas();

        // Projectile
        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentStartup>(OnGasStartup);
        SubscribeLocalEvent<GasAmmoProviderComponent, ChargeChangedEvent>(OnBatteryChargeChange);
    }

    private void OnGasStartup(EntityUid uid, GasAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void OnBatteryChargeChange(EntityUid uid, GasAmmoProviderComponent component, ChargeChangedEvent args)
    {
        UpdateShots(uid, component);
    }

    private void UpdateShots(EntityUid uid, GasAmmoProviderComponent component)
    {
        if (!TryComp<GasTankComponent>(uid, out var gasTank)) return;
        UpdateShots(component, gasTank);
    }

    private void UpdateShots(GasAmmoProviderComponent component, GasTankComponent gasTank)
    {
        var targetGas = gasTank.Air.GetMoles(component.GasId);
        //var maxShots = (int) (gastank./ component.FireCost);

        if (component.Moles != targetGas)
        {
            Dirty(component);
        }

        component.Moles = targetGas;
        UpdateGasAppearance(component);
    }

    protected override void TakeGas(EntityUid uid, GasAmmoProviderComponent component)
    {
        if (!TryComp<GasTankComponent>(uid, out var gasTank)) return;

        gasTank.Air.Remove(component.MolesPerShot);
        UpdateShots(component, gasTank);
    }
}
