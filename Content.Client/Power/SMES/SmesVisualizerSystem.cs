using Content.Shared.Power;
using Content.Shared.SMES;
using Robust.Client.GameObjects;

namespace Content.Client.Power.SMES;

public sealed class SmesVisualizerSystem : VisualizerSystem<SmesVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmesVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SmesVisualizerComponent comp, ComponentInit args)
    {
        if(!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapSet(Layers.Input, sprite.AddLayerState("smes-oc0"));
        sprite.LayerSetShader(Layers.Input, "unshaded");
        sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState("smes-og1"));
        sprite.LayerSetShader(Layers.Charge, "unshaded");
        sprite.LayerSetVisible(Layers.Charge, false);
        sprite.LayerMapSet(Layers.Output, sprite.AddLayerState("smes-op0"));
        sprite.LayerSetShader(Layers.Output, "unshaded");
    }

    protected override void OnAppearanceChange(EntityUid uid, SmesVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, SmesVisuals.LastChargeLevel, out var level, args.Component) || level == 0)
        {
            args.Sprite.LayerSetVisible(Layers.Charge, false);
        }
        else
        {
            args.Sprite.LayerSetVisible(Layers.Charge, true);
            args.Sprite.LayerSetState(Layers.Charge, $"smes-og{level}");
        }

        if (AppearanceSystem.TryGetData<ChargeState>(uid, SmesVisuals.LastChargeState, out var state, args.Component))
        {
            switch (state)
            {
                case ChargeState.Still:
                    args.Sprite.LayerSetState(Layers.Input, "smes-oc0");
                    args.Sprite.LayerSetState(Layers.Output, "smes-op1");
                    break;
                case ChargeState.Charging:
                    args.Sprite.LayerSetState(Layers.Input, "smes-oc1");
                    args.Sprite.LayerSetState(Layers.Output, "smes-op1");
                    break;
                case ChargeState.Discharging:
                    args.Sprite.LayerSetState(Layers.Input, "smes-oc0");
                    args.Sprite.LayerSetState(Layers.Output, "smes-op2");
                    break;
            }
        }
        else
        {
            args.Sprite.LayerSetState(Layers.Input, "smes-oc0");
            args.Sprite.LayerSetState(Layers.Output, "smes-op1");
        }
    }
}

enum Layers : byte
{
    Input,
    Charge,
    Output,
}
