using Content.Shared.Power;
using Content.Shared.Power.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Power.Visualizers;

/// <summary>
/// A system to update the visuals for devices using PowerNetworkBatteryComponent, e.g. SMESes and substations.
/// </summary>
public sealed partial class PowerNetworkBatteryVisualizerSystem : VisualizerSystem<PowerNetworkBatteryVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PowerNetworkBatteryVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.TryGetData<int>(PowerNetworkBatteryVisuals.LastChargeLevel, out var chargeLevel)
            && SpriteSystem.LayerMapTryGet(uid, PowerNetworkBatteryVisualLayers.ChargeLevel, out var layerIndex, logMissing: false))
        {
            if (chargeLevel == 0 && !component.ChargeLevelZeroVisible)
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, false);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, true);
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIndex, component.ChargeLevelPrefix + chargeLevel);
            }
        }

        if (args.TryGetData<ChargeState>(PowerNetworkBatteryVisuals.LastChargeState, out var chargeState))
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeState, component.ChargeStatePrefix + chargeState.ToString().ToLowerInvariant());
        }

        if (args.TryGetData<PowerNetworkBatteryChargeCapabilities>(PowerNetworkBatteryVisuals.LastChargeCapabilities, out var chargeCapabilities))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanCharge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanCharge));
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanDischarge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanDischarge));
        }
    }
}
