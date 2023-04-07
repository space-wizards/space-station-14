using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

public sealed class ApcVisualizerSystem : VisualizerSystem<ApcVisualsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ApcVisualsComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ApcVisualsComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var spriteBase = comp.SpriteStateBase;

        // Init APC frame sprite:
        sprite.LayerMapSet(ApcVisualLayers.Panel, sprite.AddLayerState($"{spriteBase}{(sbyte)ApcPanelState.Closed}"));

        // Init APC lock indicator overlays:
        for(var i = 0; i < comp.LockIndicators; ++i)
        {
            var layer = ((byte)ApcVisualLayers.LockIndicatorOverlayStart + i);
            sprite.LayerMapSet(layer, sprite.AddLayerState($"{spriteBase}{comp.LockPrefix}{i}-{(sbyte)ApcLockState.Locked}"));
            if(!string.IsNullOrWhiteSpace(comp.LockShader))
                sprite.LayerSetShader(layer, comp.LockShader);
        }

        // Init APC channel status overlays:
        for(var i = 0; i < comp.ChannelIndicators; ++i)
        {
            var layer = ((byte)ApcVisualLayers.ChannelIndicatorOverlayStart + i);
            sprite.LayerMapSet(layer, sprite.AddLayerState($"{spriteBase}{comp.ChannelPrefix}{i}-{(sbyte)ApcChannelState.AutoOn}"));
            if(!string.IsNullOrWhiteSpace(comp.ChannelShader))
                sprite.LayerSetShader(layer, comp.ChannelShader);
        }

        sprite.LayerMapSet(ApcVisualLayers.ChargeState, sprite.AddLayerState($"{spriteBase}{comp.ScreenPrefix}-{(sbyte)ApcChargeState.Lack}"));
        if(!string.IsNullOrWhiteSpace(comp.ScreenShader))
            sprite.LayerSetShader(ApcVisualLayers.ChargeState, comp.ScreenShader);
    }

    protected override void OnAppearanceChange(EntityUid uid, ApcVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        // Handle APC frame state:
        if (!AppearanceSystem.TryGetData<ApcPanelState>(uid, ApcVisuals.PanelState, out var panelState, args.Component))
            panelState = ApcPanelState.Closed;       
        args.Sprite.LayerSetState(ApcVisualLayers.Panel, $"{comp.SpriteStateBase}{(sbyte)panelState}");

        // Handle APC screen overlay:
        if(!AppearanceSystem.TryGetData<ApcChargeState>(uid, ApcVisuals.ChargeState, out var chargeState, args.Component))
            chargeState = ApcChargeState.Lack;

        if (chargeState >= 0)
        {
            args.Sprite.LayerSetState(ApcVisualLayers.ChargeState, $"{comp.SpriteStateBase}{comp.ScreenPrefix}-{(sbyte)chargeState}");

            if (AppearanceSystem.TryGetData<byte>(uid, ApcVisuals.LockState, out var lockStates, args.Component))
            {
                var spriteBase = $"{comp.SpriteStateBase}{comp.LockPrefix}";
                for(var i = 0; i < comp.LockIndicators; ++i)
                {
                    var layer = ((byte)ApcVisualLayers.LockIndicatorOverlayStart + i);
                    sbyte lockState = (sbyte)((lockStates >> (i << (sbyte)ApcLockState.LogWidth)) & (sbyte)ApcLockState.All);
                    args.Sprite.LayerSetState(layer, $"{spriteBase}{i}-{lockState}");
                    args.Sprite.LayerSetVisible(layer, true);
                }
            }

            if (AppearanceSystem.TryGetData<byte>(uid, ApcVisuals.ChannelState, out var channelStates, args.Component))
            {
                var spriteBase = $"{comp.SpriteStateBase}{comp.ChannelPrefix}";
                for(var i = 0; i < comp.ChannelIndicators; ++i)
                {
                    var layer = ((byte)ApcVisualLayers.ChannelIndicatorOverlayStart + i);
                    sbyte channelState = (sbyte)((channelStates >> (i << (sbyte)ApcChannelState.LogWidth)) & (sbyte)ApcChannelState.All);
                    args.Sprite.LayerSetState(layer, $"{spriteBase}{i}-{channelState}");
                    args.Sprite.LayerSetVisible(layer, true);
                }
            }
        }
        else
        {
            args.Sprite.LayerSetState(ApcVisualLayers.ChargeState, comp.EmaggedScreenState);
            for(var i = 0; i < comp.LockIndicators; ++i)
            {
                var layer = ((byte)ApcVisualLayers.LockIndicatorOverlayStart + i);
                args.Sprite.LayerSetVisible(layer, false);
            }
            for(var i = 0; i < comp.ChannelIndicators; ++i)
            {
                var layer = ((byte)ApcVisualLayers.ChannelIndicatorOverlayStart + i);
                args.Sprite.LayerSetVisible(layer, false);
            }
        }

        // Try to sync the color of the light produced by the APC with the color of the APCs screen:
        if (TryComp<SharedPointLightComponent>(uid, out var light))
        {
            light.Color = chargeState switch
            {
                ApcChargeState.Lack => comp.LackColor,
                ApcChargeState.Charging => comp.ChargingColor,
                ApcChargeState.Full => comp.FullColor,
                ApcChargeState.Emag => comp.EmagColor,
                _ => comp.EmagColor
            };
        }
    }
}

enum ApcVisualLayers : byte
{
    /// <summary>
    /// The sprite layer used for the APC frame.
    /// </summary>
    Panel = 0,

    /// <summary>
    /// The sprite layer used for the interface lock indicator light overlay.
    /// </summary>
    InterfaceLock = 1,
    /// <summary>
    /// The sprite layer used for the panel lock indicator light overlay.
    /// </summary>
    PanelLock = 2,
    /// <summary>
    /// The first of the lock indicator light layers.
    /// </summary>
    LockIndicatorOverlayStart = InterfaceLock,

    /// <summary>
    /// The sprite layer used for the equipment channel indicator light overlay.
    /// </summary>
    Equipment = 3,
    /// <summary>
    /// The sprite layer used for the lighting channel indicator light overlay.
    /// </summary>
    Lighting = 4,
    /// <summary>
    /// The sprite layer used for the environment channel indicator light overlay.
    /// </summary>
    Environment = 5,
    /// <summary>
    /// The first of the channel status indicator light layers.
    /// </summary>
    ChannelIndicatorOverlayStart = Equipment,

    /// <summary>
    /// The sprite layer used for the APC screen overlay.
    /// </summary>
    ChargeState = 6,
}
