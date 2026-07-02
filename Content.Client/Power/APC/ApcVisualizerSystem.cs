using Content.Shared.APC;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

public sealed partial class ApcVisualizerSystem : VisualizerSystem<ApcVisualsComponent>
{
    [Dependency] private SharedPointLightSystem _lights = default!;

    protected override void OnAppearanceChange(EntityUid uid, ApcVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // get the mapped layer index of the first lock layer and the first channel layer
        var lockIndicatorOverlayStart = SpriteSystem.LayerMapGet((uid, args.Sprite), ApcVisualLayers.InterfaceLock);

        // Handle APC screen overlay:
        if (!AppearanceSystem.TryGetData<ApcChargeState>(uid, ApcVisuals.ChargeState, out var chargeState, args.Component))
            chargeState = ApcChargeState.Lack;

        if (chargeState >= 0 && chargeState < ApcChargeState.NumStates)
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, $"{comp.ScreenPrefix}-{comp.ScreenSuffixes[(sbyte)chargeState]}");

            // LockState does nothing currently. The backend doesn't exist.
            if (AppearanceSystem.TryGetData<byte>(uid, ApcVisuals.LockState, out var lockStates, args.Component))
            {
                for (var i = 0; i < comp.LockIndicators; ++i)
                {
                    var layer = (byte)lockIndicatorOverlayStart + i;
                    var lockState = (sbyte)((lockStates >> (i << (sbyte)ApcLockState.LogWidth)) & (sbyte)ApcLockState.All);
                    SpriteSystem.LayerSetRsiState((uid, args.Sprite), layer, $"{comp.LockPrefix}{i}-{comp.LockSuffixes[lockState]}");
                    SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, true);
                }
            }

            if (AppearanceSystem.TryGetData<ApcChannelState>(uid, ApcVisuals.ChannelState, out var channelState, args.Component))
            {
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.Equipment, $"{comp.ChannelPrefix}-{comp.ChannelSuffixes[(sbyte)channelState]}");
                SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, true);
            }

            if (TryComp<PointLightComponent>(uid, out var light))
            {
                _lights.SetColor(uid, comp.ScreenColors[(sbyte)chargeState], light);
            }
        }
        else
        {
            /// Overrides all of the lock and channel indicators.
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), ApcVisualLayers.ChargeState, comp.EmaggedScreenState);
            for (var i = 0; i < comp.LockIndicators; ++i)
            {
                var layer = (byte)lockIndicatorOverlayStart + i;
                SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, false);
            }
            SpriteSystem.LayerSetVisible((uid, args.Sprite), ApcVisualLayers.Equipment, false);

            if (TryComp<PointLightComponent>(uid, out var light))
            {
                _lights.SetColor(uid, comp.EmaggedScreenColor, light);
            }
        }
    }
}

public enum ApcVisualLayers : byte
{
    /// <summary>
    /// The sprite layer used for the interface lock indicator light overlay.
    /// </summary>
    InterfaceLock,
    /// <summary>
    /// The sprite layer used for the panel lock indicator light overlay.
    /// </summary>
    PanelLock,

    /// <summary>
    /// The sprite layer used for the equipment channel indicator light overlay.
    /// </summary>
    Equipment,
    /// <summary>
    /// The sprite layer used for the lighting channel indicator light overlay.
    /// </summary>
    Lighting,
    /// <summary>
    /// The sprite layer used for the environment channel indicator light overlay.
    /// </summary>
    Environment,

    /// <summary>
    /// The sprite layer used for the APC screen overlay.
    /// </summary>
    ChargeState,
}
