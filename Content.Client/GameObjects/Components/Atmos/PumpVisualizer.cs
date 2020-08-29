using Content.Shared.GameObjects.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class PumpVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(PumpVisuals.VisualState, out PumpVisualState pumpVisualState))
            {
                return;
            }
            var pumpBaseState = "pump";
            pumpBaseState += pumpVisualState.InletDirection.ToString();
            pumpBaseState += pumpVisualState.OutletDirection.ToString();
            pumpBaseState += pumpVisualState.InletConduitLayer.ToString();
            pumpBaseState += pumpVisualState.OutletConduitLayer.ToString();

            sprite.LayerSetState(Layer.PumpBase, pumpBaseState);
        }

        private enum Layer
        {
            PumpBase
        }
    }
}
