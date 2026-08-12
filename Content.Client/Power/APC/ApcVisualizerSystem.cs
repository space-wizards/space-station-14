using Content.Shared.APC;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

public sealed partial class ApcVisualizerSystem : VisualizerSystem<ApcVisualsComponent>
{
    [Dependency] private SharedPointLightSystem _lights = default!;

    [Dependency] private EntityQuery<PointLightComponent> _pointLightQuery = default!;

    protected override void OnAppearanceChange(EntityUid uid, ApcVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Handle APC screen overlay:
        if (!AppearanceSystem.TryGetData<ApcChargeState>(uid, ApcVisuals.ChargeState, out var chargeState, args.Component))
            chargeState = ApcChargeState.Lack;

        if (chargeState >= 0 && chargeState < ApcChargeState.NumStates)
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, $"{comp.ScreenPrefix}-{comp.ScreenSuffixes[(sbyte)chargeState]}");

            if (AppearanceSystem.TryGetData<ApcChannelState>(uid, ApcVisuals.ChannelState, out var channelState, args.Component))
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.Equipment, $"{comp.ChannelPrefix}-{comp.ChannelSuffixes[(sbyte)channelState]}");
                SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, true);
            }

            if (_pointLightQuery.TryComp(uid, out var light))
                _lights.SetColor(uid, comp.ScreenColors[(sbyte)chargeState], light);
        }
        else
        {
            /// Overrides all of the lock and channel indicators.
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, comp.EmaggedScreenState);
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, false);

            if (_pointLightQuery.TryComp(uid, out var light))
                _lights.SetColor(uid, comp.EmaggedScreenColor, light);
        }
    }
}

/// <summary>
/// Sprite layers for APC visuals.
/// </summary>
public enum ApcVisualLayers : byte
{
    /// <summary>
    /// The sprite layer used for the equipment channel indicator light overlay.
    /// </summary>
    Equipment,

    /// <summary>
    /// The sprite layer used for the APC screen overlay.
    /// </summary>
    ChargeState,
}
