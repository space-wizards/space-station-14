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
            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(ClusterFlashVisualLayers.Base, sprite.AddLayerState($"{_state}-0"));
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(ClusterFlashVisuals.GrenadesMax, out int max))
            {
                max = 3;
            }

            if (component.TryGetData(ClusterFlashVisuals.GrenadesCounter, out int grenadesCounter))
            {
                var level = 0;
                if (grenadesCounter >= max){
                    level = max;
                }
                else{
                    level = grenadesCounter;
                }

                sprite.LayerSetState(ClusterFlashVisualLayers.Base, $"{_state}-{level}");
            }
        }

        private enum ClusterFlashVisualLayers : byte
        {
            Base
        }
    }
}
