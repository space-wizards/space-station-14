using System;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    /// <summary>
    /// Makes an object foldable. That means in its normal state, the object can't be picked.
    /// When clicked, it is folded, and it can now be picked and placed back. This is useful for
    /// wheelchairs, foldable chairs, rollerbeds etc.
    /// </summary>
    public abstract class SharedFoldableComponent : Component
    {
        public override string Name => "Foldable";
    }

    [Serializable, NetSerializable]
    public enum FoldableVisuals
    {
        FoldedState
    }
}
