using Content.Shared.GameObjects.Components.Atmos;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class PipeVisualizer : AppearanceVisualizer
    {
        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            if (!entity.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            sprite.LayerMapSet(Layer.PipeBase, sprite.AddLayerState("pipeFourway2")); //default
            sprite.LayerSetShader(Layer.PipeBase, "unshaded");

        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }
            if (!component.TryGetData(PipeVisuals.VisualState, out PipeVisualState pipeVisualState))
            {
                return;
            }

            var state = "pipe";
            state += pipeVisualState.PipeDirection.ToString();
            state += ((int) pipeVisualState.ConduitLayer).ToString();

            sprite.LayerSetState(Layer.PipeBase, state);
        }

        private enum Layer
        {
            PipeBase,
        }
    }
}
