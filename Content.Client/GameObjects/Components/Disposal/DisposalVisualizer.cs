using Content.Shared.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalVisualizer : AppearanceVisualizer
    {
        private string _stateAnchored;
        private string _stateUnAnchored;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            appearance.TryGetData(DisposalVisuals.Anchored, out bool anchored);

            sprite.LayerSetState(0, anchored
                ? _stateAnchored
                : _stateUnAnchored);

            if (anchored)
            {
                appearance.Owner.EnsureComponent<SubFloorHideComponent>();
            }
            else if (appearance.Owner.HasComponent<SubFloorHideComponent>())
            {
                appearance.Owner.RemoveComponent<SubFloorHideComponent>();
            }
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state_anchored", out var child))
            {
                _stateAnchored = child.AsString();
            }

            if (node.TryGetNode("state_unanchored", out child))
            {
                _stateUnAnchored = child.AsString();
            }
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
