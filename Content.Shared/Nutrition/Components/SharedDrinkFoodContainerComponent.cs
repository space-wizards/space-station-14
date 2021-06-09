#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Nutrition
{
    public abstract class SharedFoodContainerComponent : Component
    {
    }

    [NetSerializable, Serializable]
    public enum FoodContainerVisualMode
    {
        /// <summary>
        /// Discrete: 50 eggs in a carton -> down to 25, will show 12/12 until it gets below max
        /// Rounded: 50 eggs in a carton -> down to 25, will round it to 6 of 12
        /// </summary>
        Discrete,
        Rounded,
    }

    [NetSerializable, Serializable]
    public enum FoodContainerVisuals
    {
        Capacity,
        Current,
    }
}
