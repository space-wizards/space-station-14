using Robust.Client.GameObjects;
using Content.Shared.Smoking;

namespace Content.Client.Smoking;

public sealed class BurnStateVisualizerSystem : VisualizerSystem<BurnStateVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BurnStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!args.AppearanceData.TryGetValue(SmokingVisuals.Smoking, out var burnState))
            return;

        var state = burnState switch
        {
            SmokableState.Lit => component.LitIcon,
            SmokableState.Burnt => component.BurntIcon,
            _ => component.UnlitIcon
        };

        args.Sprite.LayerSetState(0, state);
    }
}

