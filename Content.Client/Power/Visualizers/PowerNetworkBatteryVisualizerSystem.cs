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

        if (AppearanceSystem.TryGetData<int>(uid, PowerNetworkBatteryVisuals.LastChargeLevel, out var chargeLevel, args.Component))
        {
            if (chargeLevel == 0 && !component.ChargeLevelZeroVisible)
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, false);
            }
            else
            {
                SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, true);
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeLevel, component.ChargeLevelPrefix + chargeLevel);
            }
        }

        if (AppearanceSystem.TryGetData<ChargeState>(uid, PowerNetworkBatteryVisuals.LastChargeState, out var chargeState, args.Component))
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), PowerNetworkBatteryVisualLayers.ChargeState, component.ChargeStatePrefix + chargeState.ToString().ToLowerInvariant());
        }

        if (AppearanceSystem.TryGetData<PowerNetworkBatteryChargeCapabilities>(uid, PowerNetworkBatteryVisuals.LastChargeCapabilities, out var chargeCapabilities, args.Component))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanCharge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanCharge));
            SpriteSystem.LayerSetVisible((uid, args.Sprite), PowerNetworkBatteryVisualLayers.CanDischarge, chargeCapabilities.HasFlag(PowerNetworkBatteryChargeCapabilities.CanDischarge));
        }
    }
}
