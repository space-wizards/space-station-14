using Content.Shared.GameObjects.Components.Explosion;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Explosion
{
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class ClusterFlashVisualizer : AppearanceVisualizer
    {
        private string _state;

        private enum ClusterFlashVisualLayers
        {
            Base
        }
        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("state", out var child))
            {
                _state = child.AsString();
            }
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(ClusterFlashVisualLayers.Base, sprite.AddLayerState(_state));
        }


        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData(ClusterFlashVisuals.GrenadesCounter, out int grenadesCounter))
            {
                switch (grenadesCounter){
                    case 0:
                        sprite.LayerSetState(ClusterFlashVisualLayers.Base, "empty");
                        break;
                    case 1:
                        sprite.LayerSetState(ClusterFlashVisualLayers.Base, "one");
                        break;
                    case 2:
                        sprite.LayerSetState(ClusterFlashVisualLayers.Base, "two");
                        break;
                    case 3:
                        sprite.LayerSetState(ClusterFlashVisualLayers.Base, "three");
                        break;
                    default:
                        sprite.LayerSetState(ClusterFlashVisualLayers.Base, "three");
                        break;
                }
            }
        }

    }
}
