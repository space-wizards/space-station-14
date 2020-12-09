using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public sealed class DrinkFoodVisualizer : AppearanceVisualizer
    {
        private int _steps;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _steps = node.GetNode("steps").AsInt();
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData<float>(SharedFoodComponent.FoodVisuals.MaxUses, out var maxUses))
            {
                return;
            }

            if (component.TryGetData<float>(SharedFoodComponent.FoodVisuals.Visual, out var usesLeft))
            {
                var step = ContentHelpers.RoundToLevels(usesLeft, maxUses, _steps);
                sprite.LayerSetState(0, $"icon-{step}");
            }
            else
            {
                sprite.LayerSetState(0, "icon-0");
            }
        }
    }
}
