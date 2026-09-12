using Content.Shared.APC;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

/// <summary>
/// A system to update the screen and the channel indicators for an APC.
/// </summary>
public sealed partial class ApcVisualizerSystem : VisualizerSystem<ApcVisualsComponent>
{
    [Dependency] private SharedPointLightSystem _lights = default!;

    [Dependency] private EntityQuery<PointLightComponent> _pointLightQuery = default!;

    /// <inheritdoc/>
    protected override void OnAppearanceChange(EntityUid uid, ApcVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Handle APC screen overlay and channel markers.
        if (!args.TryGetData<ApcChargeState>(ApcVisuals.ChargeState, out var chargeState))
            chargeState = ApcChargeState.Lack;

        if (chargeState < ApcChargeState.NumStates)
        {
            var screenState = comp.ScreenStateSuffixes[(byte)chargeState] is { } screenSuffix ? $"{comp.ScreenStatePrefix}-{screenSuffix}" : null;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, screenState);

            // Unlike the charge state, we don't have an emag with special visuals, everything's in the array.
            if (!args.TryGetData<ApcChannelState>(ApcVisuals.ChannelState, out var channelState)
                || channelState >= ApcChannelState.NumStates)
            {
                channelState = ApcChannelState.Off;
            }

            var state = comp.ChannelIndicatorSuffixes[(byte)channelState] is { } channelSuffix ? $"{comp.ChannelIndicatorPrefix}-{channelSuffix}" : null;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.Equipment, state);
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, true);

            if (_pointLightQuery.TryComp(uid, out var light))
                _lights.SetColor(uid, comp.ScreenColors[(byte)chargeState], light);
        }
        else
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, comp.EmaggedScreenState);
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, false);

            if (_pointLightQuery.TryComp(uid, out var light))
                _lights.SetColor(uid, comp.EmaggedScreenColor, light);
        }
    }
}
