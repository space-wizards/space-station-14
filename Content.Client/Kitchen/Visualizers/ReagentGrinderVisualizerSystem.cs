using Robust.Client.GameObjects;
using Content.Shared.Kitchen;

namespace Content.Client.Kitchen.Visualizers;

public sealed class ReagentGrinderVisualizerSystem : VisualizerSystem<ReagentGrinderVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ReagentGrinderVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        AppearanceSystem.TryGetData(uid, ReagentGrinderVisualState.BeakerAttached, out bool hasBeaker, args.Component);
        args.Sprite.LayerSetState(0, $"juicer{(hasBeaker ? "1" : "0")}");
    }
}
