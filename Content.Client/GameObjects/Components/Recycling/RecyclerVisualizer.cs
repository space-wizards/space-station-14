using Content.Shared.GameObjects.Components.Recycling;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Recycling
{
    [UsedImplicitly]
    public class RecyclerVisualizer : AppearanceVisualizer
    {
        private string _stateClean;
        private string _stateBloody;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state_clean", out var child))
            {
                _stateClean = child.AsString();
            }

            if (node.TryGetNode("state_bloody", out child))
            {
                _stateBloody = child.AsString();
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

            appearance.TryGetData(RecyclerVisuals.Bloody, out bool bloody);
            sprite.LayerSetState(RecyclerVisualLayers.Bloody, bloody
                ? _stateBloody
                : _stateClean);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            component.TryGetData(RecyclerVisuals.Bloody, out bool bloody);
            sprite.LayerSetState(RecyclerVisualLayers.Bloody, bloody
                ? _stateBloody
                : _stateClean);
        }
    }

    public enum RecyclerVisualLayers : byte
    {
        Bloody
    }
}
