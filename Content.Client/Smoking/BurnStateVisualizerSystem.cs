using Content.Shared.Smoking;
using Robust.Client.GameObjects;

namespace Content.Client.Smoking;

public sealed class BurnStateVisualizerSystem : VisualizerSystem<BurnStateVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BurnStateVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if(!AppearanceSystem.TryGetData<SmokableState>(uid, SmokingVisuals.Smoking, out var burnState, args.Component))
            return;

        var state = burnState switch
        {
            SmokableState.Lit => comp.LitIcon,
            SmokableState.Burnt => comp.BurntIcon,
            _ => comp.UnlitIcon
        };

        args.Sprite.LayerSetState(0, state);
    }
}
