using Content.Client.SubFloor;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Power.Visualizers;

/// <summary>
/// A system to update the visuals for devices using PowerNetworkBatteryComponent, e.g. SMESes and substations.
/// </summary>
public sealed partial class PowerNetworkBatteryVisualizerSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerNetworkBatteryVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(SubFloorHideSystem) });
    }

    private void OnAppearanceChange(EntityUid uid, PowerNetworkBatteryVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearanceSystem.TryGetData<int>(uid, PowerNetworkBatteryVisuals.LastChargeLevel, out var chargeLevel, args.Component))
        {
            if (chargeLevel == 0 && !component.ChargeLevelZeroVisible)
            {
                _sprite.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, false);
            }
            else
            {
                _sprite.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, true);
                _sprite.LayerSetRsiState((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, component.ChargeLevelPrefix + chargeLevel);
            }
        }

        if (_appearanceSystem.TryGetData<ChargeState>(uid, PowerNetworkBatteryVisuals.LastChargeState, out var chargeState, args.Component))
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeState, component.ChargeStatePrefix + chargeState.ToString().ToLowerInvariant());
        }

        if (_appearanceSystem.TryGetData<PowerNetworkBatteryChargeCapabilities>(uid, PowerNetworkBatteryVisuals.LastChargeCapabilities, out var chargeCapabilities, args.Component))
        {
            _sprite.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanCharge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanCharge));
            _sprite.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanDischarge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanDischarge));
        }
    }
}
