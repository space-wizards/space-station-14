using Content.Shared.Weapons.Ranged;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);

        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
    }

    private void OnAmmoCountUpdate(EntityUid uid, BatteryAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes) return;

        boxes.Update(component.Shots, component.MaxShots);
    }

    private void OnControl(EntityUid uid, BatteryAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }
}
