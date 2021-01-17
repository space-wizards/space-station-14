using Content.Shared.GameObjects.Components.Explosion;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Explosion
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class ClusterFlashVisualizer : AppearanceVisualizer
    {
        private int _levels;
        private string _state;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("state", out var state))
            {
                _state = state.AsString();
            }

            if (node.TryGetNode("levels", out var levels))
            {
                _levels = levels.AsInt();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            if (entity.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                sprite.LayerMapSet(ClusterFlashVisualLayers.Base, sprite.AddLayerState($"{_state}-0"));
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent<ISpriteComponent>(out var sprite))
            {
                return;
            }

            if (!component.TryGetData(ClusterFlashVisuals.GrenadesMax, out byte max))
            {
                max = 3;
            }

            if (component.TryGetData(ClusterFlashVisuals.GrenadesCounter, out byte grenadesCounter))
            {
                var level = ContentHelpers.RoundToLevels(grenadesCounter, max, _levels);

                sprite.LayerSetState(ClusterFlashVisualLayers.Base, $"{_state}-{level}");
            }
        }

        private enum ClusterFlashVisualLayers : byte
        {
            Base
        }
    }
}
