using System;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [UsedImplicitly]
    public sealed class FoodContainerVisualizer : AppearanceVisualizer
    {
        [DataField("base_state", required: true)]
        private string? _baseState;

        [DataField("steps", required: true)]
        private int _steps;

        [DataField("mode")]
        private FoodContainerVisualMode _mode = FoodContainerVisualMode.Rounded;

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            if (!component.TryGetData<int>(FoodContainerVisuals.Current, out var current))
            {
                return;
            }

            if (!component.TryGetData<int>(FoodContainerVisuals.Capacity, out var capacity))
            {
                return;
            }

            int step;

            switch (_mode)
            {
                case FoodContainerVisualMode.Discrete:
                    step = Math.Min(_steps - 1, current);
                    break;
                case FoodContainerVisualMode.Rounded:
                    step = ContentHelpers.RoundToLevels(current, capacity, _steps);
                    break;
                default:
                    throw new NullReferenceException();
            }

            sprite.LayerSetState(0, $"{_baseState}-{step}");
        }
    }
}
