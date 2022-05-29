using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    protected virtual void InitializeBattery()
    {
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentGetState>(OnBatteryGetState);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ComponentHandleState>(OnBatteryHandleState);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, TakeAmmoEvent>(OnBatteryTakeAmmo);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, GetAmmoCountEvent>(OnBatteryAmmoCount);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, ExaminedEvent>(OnBatteryExamine);
    }

    private void OnBatteryHandleState(EntityUid uid, BatteryAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BatteryAmmoProviderComponentState state) return;

        component.Shots = state.Shots;
        component.MaxShots = state.MaxShots;
        component.FireCost = state.FireCost;
    }

    private void OnBatteryGetState(EntityUid uid, BatteryAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new BatteryAmmoProviderComponentState()
        {
            Shots = component.Shots,
            MaxShots = component.MaxShots,
            FireCost = component.FireCost,
        };
    }

    private void OnBatteryExamine(EntityUid uid, BatteryAmmoProviderComponent component, ExaminedEvent args)
    {
        args.PushMarkup($"It has enough charge for [color={AmmoExamineColor}]{component.Shots} shots.");
    }

    private void OnBatteryTakeAmmo(EntityUid uid, BatteryAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, component.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0) return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetShootable(component, args.Coordinates));
            component.Shots--;
        }

        TakeCharge(uid, component);
        UpdateBatteryAppearance(uid, component);
        Dirty(component);
    }

    private void OnBatteryAmmoCount(EntityUid uid, BatteryAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = component.Shots;
        args.Capacity = component.MaxShots;
    }

    /// <summary>
    /// Update the battery (server-only) whenever fired.
    /// </summary>
    protected virtual void TakeCharge(EntityUid uid, BatteryAmmoProviderComponent component) {}

    protected void UpdateBatteryAppearance(EntityUid uid, BatteryAmmoProviderComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;
        appearance.SetData(AmmoVisuals.AmmoCount, component.Shots);
        appearance.SetData(AmmoVisuals.AmmoMax, component.MaxShots);
    }

    private IShootable GetShootable(BatteryAmmoProviderComponent component, EntityCoordinates coordinates)
    {
        switch (component)
        {
            case ProjectileBatteryAmmoProviderComponent proj:
                var ent = Spawn(proj.Prototype, coordinates);
                return EnsureComp<NewAmmoComponent>(ent);
            case HitscanBatteryAmmoProviderComponent hitscan:
                return ProtoManager.Index<HitscanPrototype>(hitscan.Prototype);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [Serializable, NetSerializable]
    private sealed class BatteryAmmoProviderComponentState : ComponentState
    {
        public int Shots;
        public int MaxShots;
        public float FireCost;
    }
}
