using Content.Shared.GameObjects.Atmos;
using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class PumpVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            sprite.LayerMapSet(Layer.PumpBase, sprite.AddLayerState("pumpSouthNorth2")); //default
            sprite.LayerSetShader(Layer.PumpBase, "unshaded");
        }

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
