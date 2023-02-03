using Content.Shared.Buckle.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Visualizer;

public sealed class RollerbedVisualizerSystem : VisualizerSystem<RollerbedVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, RollerbedVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, StrapVisuals.State, out bool strapped, args.Component) && strapped)
        {
            args.Sprite.LayerSetState(0, $"{comp.Key}_buckled");
        }
    }
}
