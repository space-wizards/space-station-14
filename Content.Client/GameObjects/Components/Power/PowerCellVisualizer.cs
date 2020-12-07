using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    public class PowerCellVisualizer : AppearanceVisualizer
    {
        private string _prefix;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _prefix = node.GetNode("prefix").AsString();
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var sprite = entity.GetComponent<ISpriteComponent>();

            sprite.LayerMapSet(Layers.Charge, sprite.AddLayerState($"{_prefix}_100"));
            sprite.LayerSetShader(Layers.Charge, "unshaded");
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData(PowerCellVisuals.ChargeLevel, out byte level))
            {
                var adjustedLevel = level * 25;
                sprite.LayerSetState(Layers.Charge, $"{_prefix}_{adjustedLevel}");
            }
        }

        private enum Layers : byte
        {
            Charge
        }
    }
}
