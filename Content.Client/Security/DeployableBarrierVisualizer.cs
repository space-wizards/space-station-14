using Content.Shared.Security;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Security
{
    [UsedImplicitly]
    public sealed class DeployableBarrierVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
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
