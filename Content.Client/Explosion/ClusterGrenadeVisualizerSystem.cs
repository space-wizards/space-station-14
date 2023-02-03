using Content.Shared.Explosion;
using Robust.Client.GameObjects;

namespace Content.Client.Explosion;

public sealed class ClusterGrenadeVisualizerSystem : VisualizerSystem<ClusterGrenadeVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ClusterGrenadeVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData(uid, ClusterGrenadeVisuals.GrenadesCounter, out int grenadesCounter, args.Component))
            args.Sprite.LayerSetState(0, $"{comp.State}-{grenadesCounter}");
    }
}
