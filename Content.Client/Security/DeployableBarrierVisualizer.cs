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
                    ToggleLight(component, false);
                    break;
                case DeployableBarrierState.Deployed:
                    sprite.LayerSetState(0, "deployed");
                    ToggleLight(component, true);
                    break;
            }
        }

        private void ToggleLight(AppearanceComponent component, bool enabled)
        {
            if (component.Owner.TryGetComponent(out PointLightComponent? light))
                light.Enabled = enabled;
        }
    }
}
