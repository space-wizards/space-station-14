using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Utility;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Power
{
    public class HitscanWeaponVisualizer2D : AppearanceVisualizer
    {
        private string _prefix;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _prefix = node.GetNode("prefix").AsString();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (component.TryGetData(PowerCellVisuals.ChargeLevel, out float fraction))
            {
                sprite.LayerSetState(0, $"{_prefix}_{ContentHelpers.RoundToLevels(fraction, 1, 5) * 25}");
            }
        }
    }
}
