using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Foldable
{
    /// <summary>
    /// Foldable component allows you to "fold" an entity and pick it up as an item. First made for rollerbeds and wheelchairs
    /// </summary>
    public class SharedFoldableComponent : Component
    {
        public override string Name => "Foldable";

        [Serializable, NetSerializable]
        public enum FoldableVisuals
        {
            FoldedState
        }
    }
}
