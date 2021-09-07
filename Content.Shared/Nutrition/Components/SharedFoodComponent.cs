using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    public class SharedFoodComponent
    {
        // TODO: Remove maybe? Add visualizer for food
        [Serializable, NetSerializable]
        public enum FoodVisuals
        {
            Visual,
            MaxUses,
        }
    }
}
