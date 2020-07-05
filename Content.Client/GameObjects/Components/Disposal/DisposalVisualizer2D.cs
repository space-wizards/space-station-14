using Content.Shared.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalVisualizer2D : AppearanceVisualizer
    {
        private string _stateAnchored;
        private string _stateUnAnchored;

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

            if (!entity.TryGetComponent(out ISpriteComponent sprite) ||
                !entity.TryGetComponent(out AppearanceComponent appearance))
            {
                return;
            }

            appearance.TryGetData(DisposalVisuals.Anchored, out bool anchored);
            sprite.LayerSetState(0, anchored
                ? _stateAnchored
                : _stateUnAnchored);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            component.TryGetData(DisposalVisuals.Anchored, out bool anchored);
            sprite.LayerSetState(0, anchored
                ? _stateAnchored
                : _stateUnAnchored);
        }
    }
}
