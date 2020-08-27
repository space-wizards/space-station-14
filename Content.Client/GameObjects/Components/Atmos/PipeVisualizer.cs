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
        private PipeDirection _pipeDirection;
        private int _conduitLayer;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            if (!appearance.TryGetData(PipeVisuals.VisualState, out PipeVisualState pipeVisualState))
            {
                return;
            }

            var state = "pipe";
            state += pipeVisualState.PipeDirection.ToString();
            state += pipeVisualState.ConduitLayer.ToString();

            sprite.LayerSetState(0, state);
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var appearance = entity.EnsureComponent<AppearanceComponent>();
            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.Owner.Deleted)
            {
                return;
            }

            ChangeState(component);
        }
    }
}
