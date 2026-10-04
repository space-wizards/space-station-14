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

        // Update charge level.
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

        // Update charge state.
        if (args.TryGetData<ChargeState>(PowerNetworkBatteryVisuals.LastChargeState, out var chargeState)
            && SpriteSystem.LayerMapTryGet(uid, PowerNetworkBatteryVisualLayers.ChargeState, out layerIndex, logMissing: false))
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), layerIndex, component.ChargeStatePrefix + chargeState.ToString().ToLowerInvariant());
        }

        // Update charge capabilities.
        if (args.TryGetData<PowerNetworkBatteryChargeCapabilities>(PowerNetworkBatteryVisuals.LastChargeCapabilities, out var chargeCapabilities))
        {
            if (SpriteSystem.LayerMapTryGet(uid, PowerNetworkBatteryVisualLayers.CanCharge, out layerIndex, logMissing: false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanCharge));

            if (SpriteSystem.LayerMapTryGet(uid, PowerNetworkBatteryVisualLayers.CanDischarge, out layerIndex, logMissing: false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layerIndex, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanDischarge));
        }
    }
}
