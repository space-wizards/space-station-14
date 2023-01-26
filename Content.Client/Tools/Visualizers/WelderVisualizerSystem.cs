using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Tools.Visualizers;

public sealed class WelderVisualizerSystem : VisualizerSystem<WelderComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    protected override void OnAppearanceChange(EntityUid uid, WelderComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData(uid, WelderVisuals.Lit, out bool isLit, args.Component))
        {
            args.Sprite.LayerSetVisible(WelderLayers.Flame, isLit);
        }
    }
}
