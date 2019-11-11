using System;
using System.Collections.Generic;
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
    public sealed class DrinkFoodContainerVisualizer2D : AppearanceVisualizer
    {
        private string _baseState;
        private int _steps;
        private DrinkFoodContainerVisualMode _mode;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _baseState = node.GetNode("base_state").AsString();
            _steps = node.GetNode("steps").AsInt();
            try
            {
                _mode = node.GetNode("mode").AsEnum<DrinkFoodContainerVisualMode>();
            }
            catch (KeyNotFoundException)
            {
                _mode = DrinkFoodContainerVisualMode.Rounded;
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData<int>(DrinkFoodContainerVisuals.Current, out var current))
            {
                return;
            }

            if (!component.TryGetData<int>(DrinkFoodContainerVisuals.Capacity, out var capacity))
            {
                return;
            }

            int step;

            switch (_mode)
            {
                case DrinkFoodContainerVisualMode.Discrete:
                    step = Math.Min(_steps - 1, current);
                    break;
                case DrinkFoodContainerVisualMode.Rounded:
                    step = ContentHelpers.RoundToLevels(current, capacity, _steps);
                    break;
                default:
                    throw new NullReferenceException();
            }

            sprite.LayerSetState(0, $"{_baseState}-{step}");
        }
    }
}
