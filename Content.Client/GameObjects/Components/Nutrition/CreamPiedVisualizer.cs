using Content.Shared.GameObjects.Components.Nutrition;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public class CreamPiedVisualizer : AppearanceVisualizer
    {
        private string _state;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapReserveBlank(CreamPiedVisualLayers.Pie);
            sprite.LayerSetRSI(CreamPiedVisualLayers.Pie, "Effects/creampie.rsi");
            sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, false);
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state", out var otherNode))
            {
                _state = otherNode.AsString();
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData<bool>(CreamPiedVisuals.Creamed, out var pied))
            {
                SetPied(component, pied);
            }
        }

        private void SetPied(AppearanceComponent component, bool pied)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            sprite.LayerSetVisible(CreamPiedVisualLayers.Pie, pied);
            sprite.LayerSetState(CreamPiedVisualLayers.Pie, _state);
        }
    }

    public enum CreamPiedVisualLayers : byte
    {
        Pie,
    }
}
