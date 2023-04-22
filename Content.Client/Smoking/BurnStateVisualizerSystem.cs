using Robust.Client.GameObjects;
using Content.Shared.Smoking;

namespace Content.Client.Smoking;

public sealed class BurnStateVisualizerSystem : VisualizerSystem<BurnStateVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, BurnStateVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if(!args.AppearanceData.TryGetValue(SmokingVisuals.Smoking, out var burnState))
            return;

        var state = burnState switch
        {
            SmokableState.Lit => component._litIcon,
            SmokableState.Burnt => component._burntIcon,
            _ => component._unlitIcon
        };

        args.Sprite?.LayerSetState(0, state);
    }
}

