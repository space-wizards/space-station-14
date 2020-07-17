using System;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Buckle
{
    public abstract class SharedBuckleComponent : Component, IActionBlocker, IEffectBlocker
    {
        public sealed override string Name => "Buckle";

        public sealed override uint? NetID => ContentNetIDs.BUCKLE;

        /// <summary>
        ///     True if the entity is buckled, false otherwise.
        /// </summary>
        public abstract bool Buckled { get; }

        bool IActionBlocker.CanMove()
        {
            return !Buckled;
        }

        bool IActionBlocker.CanChangeDirection()
        {
            return !Buckled;
        }

        bool IEffectBlocker.CanFall()
        {
            return !Buckled;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BuckleComponentState : ComponentState
    {
        public BuckleComponentState(bool buckled, int? drawDepth) : base(ContentNetIDs.BUCKLE)
        {
            Buckled = buckled;
            DrawDepth = drawDepth;
        }

        public bool Buckled { get; }
        public int? DrawDepth;
    }

    [Serializable, NetSerializable]
    public enum BuckleVisuals
    {
        Buckled
    }
}
