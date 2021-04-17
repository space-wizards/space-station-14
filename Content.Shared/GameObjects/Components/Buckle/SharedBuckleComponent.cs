#nullable enable
using System;
using Content.Shared.GameObjects.Components.Strap;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Buckle
{
    public abstract class SharedBuckleComponent : Component, IActionBlocker, IEffectBlocker, IDraggable, ICollideSpecial
    {
        public sealed override string Name => "Buckle";

        public sealed override uint? NetID => ContentNetIDs.BUCKLE;

        [ComponentDependency] protected readonly IPhysBody? Physics;

        /// <summary>
        ///     The range from which this entity can buckle to a <see cref="SharedStrapComponent"/>.
        /// </summary>
        [ViewVariables]
        [DataField("range")]
        public float Range { get; protected set; } = SharedInteractionSystem.InteractionRange / 1.4f;

        /// <summary>
        ///     True if the entity is buckled, false otherwise.
        /// </summary>
        public abstract bool Buckled { get; }

        public EntityUid? LastEntityBuckledTo { get; set; }

        public bool IsOnStrapEntityThisFrame { get; set; }

        public bool DontCollide { get; set; }

        public abstract bool TryBuckle(IEntity? user, IEntity to);

        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            if (collidedwith.Owner.Uid == LastEntityBuckledTo)
            {
                IsOnStrapEntityThisFrame = true;
                return Buckled || DontCollide;
            }

            return false;
        }

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

        bool IDraggable.CanDrop(CanDropEventArgs args)
        {
            return args.Target.HasComponent<SharedStrapComponent>();
        }

        bool IDraggable.Drop(DragDropEventArgs args)
        {
            return TryBuckle(args.User, args.Target);
        }
    }

    [Serializable, NetSerializable]
    public sealed class BuckleComponentState : ComponentState
    {
        public BuckleComponentState(bool buckled, int? drawDepth, EntityUid? lastEntityBuckledTo, bool dontCollide) : base(ContentNetIDs.BUCKLE)
        {
            Buckled = buckled;
            DrawDepth = drawDepth;
            LastEntityBuckledTo = lastEntityBuckledTo;
            DontCollide = dontCollide;
        }

        public bool Buckled { get; }
        public EntityUid? LastEntityBuckledTo { get; }
        public bool DontCollide { get; }
        public int? DrawDepth;
    }

    [Serializable, NetSerializable]
    public enum BuckleVisuals
    {
        Buckled
    }

    [Serializable, NetSerializable]
    public abstract class BuckleChangeMessage : ComponentMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="BuckleChangeMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        /// <param name="buckled">True if the entity was buckled, false otherwise</param>
        protected BuckleChangeMessage(IEntity entity, IEntity strap, bool buckled)
        {
            Entity = entity;
            Strap = strap;
            Buckled = buckled;
        }

        /// <summary>
        ///     The entity that had its buckling status changed
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        ///     The strap that the entity was buckled to or unbuckled from
        /// </summary>
        public IEntity Strap { get; }

        /// <summary>
        ///     True if the entity was buckled, false otherwise.
        /// </summary>
        public bool Buckled { get; }
    }

    [Serializable, NetSerializable]
    public class BuckleMessage : BuckleChangeMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="BuckleMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        public BuckleMessage(IEntity entity, IEntity strap) : base(entity, strap, true)
        {
        }
    }

    [Serializable, NetSerializable]
    public class UnbuckleMessage : BuckleChangeMessage
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="UnbuckleMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        public UnbuckleMessage(IEntity entity, IEntity strap) : base(entity, strap, false)
        {
        }
    }
}
