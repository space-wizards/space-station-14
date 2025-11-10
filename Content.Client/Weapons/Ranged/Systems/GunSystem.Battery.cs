using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        SubscribeLocalEvent<BatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
    }

    private void OnAmmoCountUpdate(EntityUid uid, BatteryAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes) return;

        // This returns 0 for non-predicted Batteries as those are not networked.
        // But that does not matter, since all handheld guns should be using PredictedBatteryComponent anyways.
        (var shots, var capacity) = GetShots((uid, component));
        boxes.Update(shots, capacity);
    }

    private void OnControl(EntityUid uid, BatteryAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }
}
