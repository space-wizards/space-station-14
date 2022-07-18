using Content.Server.Atmos.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeGas()
    {
        base.InitializeGas();

        SubscribeLocalEvent<GasAmmoProviderComponent, ComponentStartup>(OnGasStartup);
    }

    private void OnGasStartup(EntityUid uid, GasAmmoProviderComponent component, ComponentStartup args)
    {
        UpdateShots(uid, component);
    }

    private void UpdateShots(EntityUid uid, GasAmmoProviderComponent component)
    {
        if (!TryComp<GasTankComponent>(component.TankEntity, out var gasTank)) return;
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

    protected override void UpdateGas(EntityUid uid, GasAmmoProviderComponent component, bool shotFired)
    {
        if (!TryComp<GasTankComponent>(component.TankEntity, out var gasTank)) return;

        if (shotFired)
            gasTank.Air.AdjustMoles(component.GasId, -component.MolesPerShot);
        UpdateShots(component, gasTank);
    }
}
