using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power.APC;

public sealed class ApcVisualizerSystem : VisualizerSystem<ApcVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ApcVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ApcVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapSet(Layers.Panel, sprite.AddLayerState("apc0"));

        sprite.LayerMapSet(Layers.ChargeState, sprite.AddLayerState("apco3-0"));
        sprite.LayerSetShader(Layers.ChargeState, "unshaded");

        sprite.LayerMapSet(Layers.Lock, sprite.AddLayerState("apcox-0"));
        sprite.LayerSetShader(Layers.Lock, "unshaded");

        sprite.LayerMapSet(Layers.Equipment, sprite.AddLayerState("apco0-3"));
        sprite.LayerSetShader(Layers.Equipment, "unshaded");

        sprite.LayerMapSet(Layers.Lighting, sprite.AddLayerState("apco1-3"));
        sprite.LayerSetShader(Layers.Lighting, "unshaded");

        sprite.LayerMapSet(Layers.Environment, sprite.AddLayerState("apco2-3"));
        sprite.LayerSetShader(Layers.Environment, "unshaded");
    }

    protected override void OnAppearanceChange(EntityUid uid, ApcVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        if (AppearanceSystem.TryGetData<ApcPanelState>(uid, ApcVisuals.PanelState, out var panelState, args.Component))
        {
            switch (panelState)
            {
                case ApcPanelState.Closed:
                    args.Sprite.LayerSetState(Layers.Panel, "apc0");
                    break;
                case ApcPanelState.Open:
                    args.Sprite.LayerSetState(Layers.Panel, "apcframe");
                    break;
            }
        }
        if (AppearanceSystem.TryGetData<ApcChargeState>(uid, ApcVisuals.ChargeState, out var chargeState, args.Component))
        {
            switch (chargeState)
            {
                case ApcChargeState.Lack:
                    args.Sprite.LayerSetState(Layers.ChargeState, "apco3-0");
                    break;
                case ApcChargeState.Charging:
                    args.Sprite.LayerSetState(Layers.ChargeState, "apco3-1");
                    break;
                case ApcChargeState.Full:
                    args.Sprite.LayerSetState(Layers.ChargeState, "apco3-2");
                    break;
                case ApcChargeState.Emag:
                    args.Sprite.LayerSetState(Layers.ChargeState, "emag-unlit");
                    break;
            }

            if (TryComp(uid, out SharedPointLightComponent? light))
            {
                light.Color = chargeState switch
                {
                    ApcChargeState.Lack => ApcVisualizerComponent.LackColor,
                    ApcChargeState.Charging => ApcVisualizerComponent.ChargingColor,
                    ApcChargeState.Full => ApcVisualizerComponent.FullColor,
                    ApcChargeState.Emag => ApcVisualizerComponent.EmagColor,
                    _ => ApcVisualizerComponent.LackColor
                };
            }
        }
        else
        {
            args.Sprite.LayerSetState(Layers.ChargeState, "apco3-0");
        }
    }
}

enum Layers : byte
{
    ChargeState,
    Lock,
    Equipment,
    Lighting,
    Environment,
    Panel,
}
