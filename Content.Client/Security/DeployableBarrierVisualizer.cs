using Content.Shared.Security;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Security
{
    [UsedImplicitly]
    public class DeployableBarrierVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? sprite))
                return;

            if (!component.TryGetData(DeployableBarrierVisuals.State, out DeployableBarrierState state))
                return;

            switch (state)
            {
                case DeployableBarrierState.Idle:
                    sprite.LayerSetState(0, "idle");
                    break;
                case DeployableBarrierState.Deployed:
                    sprite.LayerSetState(0, "deployed");
                    break;
            }
        }
    }
}
