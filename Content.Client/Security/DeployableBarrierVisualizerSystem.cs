using Content.Shared.Security;
using Robust.Client.GameObjects;

namespace Content.Client.Security;

public sealed class DeployableBarrierVisualizerSystem : VisualizerSystem<DeployableBarrierVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DeployableBarrierVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        
        if (!AppearanceSystem.TryGetData(uid, DeployableBarrierVisuals.State, out DeployableBarrierState state, args.Component))
            return;

        switch (state)
        {
            case DeployableBarrierState.Idle:
                args.Sprite.LayerSetState(0, "idle");
                break;
            case DeployableBarrierState.Deployed:
                args.Sprite.LayerSetState(0, "deployed");
                break;
        }
    }
}
