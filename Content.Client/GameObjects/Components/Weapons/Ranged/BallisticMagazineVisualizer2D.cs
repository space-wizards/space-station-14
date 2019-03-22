using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Utility;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    public sealed class BallisticMagazineVisualizer2D : AppearanceVisualizer
    {
        private string _baseState;
        private int _steps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _baseState = node.GetNode("base_state").AsString();
            _steps = node.GetNode("steps").AsInt();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData(BallisticMagazineVisuals.AmmoCapacity, out int capacity))
            {
                return;
            }
            if (!component.TryGetData(BallisticMagazineVisuals.AmmoLeft, out int current))
            {
                return;
            }

            var step = ContentHelpers.RoundToLevels(current, capacity, _steps);

            sprite.LayerSetState(0, $"{_baseState}-{step}");
        }
    }
}
