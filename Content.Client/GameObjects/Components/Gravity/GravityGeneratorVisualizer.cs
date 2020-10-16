#nullable enable
using Content.Shared.GameObjects.Components.Gravity;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Gravity
{
    public class GravityGeneratorVisualizer : AppearanceVisualizer
    {
        private int _coreLayer = 1;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("coreLayer", out var coreLayer))
            {
                _coreLayer = coreLayer.AsInt();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<SpriteComponent>();

            if (component.TryGetData(GravityGeneratorVisuals.State, out string? state))
            {
                sprite.LayerSetState(0, state);
            }

            if (component.TryGetData(GravityGeneratorVisuals.CoreVisible, out bool visible))
            {
                sprite.LayerSetVisible(_coreLayer, visible);
            }
        }
    }
}
