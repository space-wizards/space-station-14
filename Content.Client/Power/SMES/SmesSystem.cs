using Content.Shared.Power;
using Content.Shared.SMES;
using Robust.Client.GameObjects;

namespace Content.Client.Power.SMES;

public sealed class SmesVisualizerSystem : VisualizerSystem<SmesComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SmesComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, SmesVisuals.LastChargeLevel, out var level, args.Component) || level == 0)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), SmesVisualLayers.Charge, false);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), SmesVisualLayers.Charge, true);
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Charge, $"{comp.ChargeOverlayPrefix}{level}");
        }

        if (!AppearanceSystem.TryGetData<ChargeState>(uid, SmesVisuals.LastChargeState, out var state, args.Component))
            state = ChargeState.Still;

        switch (state)
        {
            case ChargeState.Still:
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Input, $"{comp.InputOverlayPrefix}0");
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Output, $"{comp.OutputOverlayPrefix}1");
                break;
            case ChargeState.Charging:
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Input, $"{comp.InputOverlayPrefix}1");
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Output, $"{comp.OutputOverlayPrefix}1");
                break;
            case ChargeState.Discharging:
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Input, $"{comp.InputOverlayPrefix}0");
                SpriteSystem.LayerSetRsiState((uid, args.Sprite), SmesVisualLayers.Output, $"{comp.OutputOverlayPrefix}2");
                break;
        }
    }
}

public enum SmesVisualLayers : byte
{
    Input,
    Charge,
    Output,
}
