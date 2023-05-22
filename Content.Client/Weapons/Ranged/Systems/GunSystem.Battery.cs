using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
    }

    private void OnAmmoCountUpdate(EntityUid uid, BatteryAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes)
            return;

        boxes.Update(component.Shots, component.Capacity);

        SetCollisionForAmmoBattery(uid, component);
    }

    private void OnControl(EntityUid uid, BatteryAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }

    private void SetCollisionForAmmoBattery(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (GetCurrentPlayerGunForAnyMode() is not GunComponent gun)
            return;

        if (component.Shots == 0 && gun.NexFireCollisionMask != null)
        {
            gun.NexFireCollisionMask = null;
            return;
        }

        if (component.Shots == 0 || gun.NexFireCollisionMask != null)
            return;

        switch (component)
        {
            case ProjectileBatteryAmmoProviderComponent proj:
                SetCollisionMaskForPrototype(uid);
                break;
            case HitscanBatteryAmmoProviderComponent hitscan:
                gun.NexFireCollisionMask =
                    (ProtoManager.Index<HitscanPrototype>(hitscan.Prototype)).CollisionMask;
                break;
        }
    }
}
