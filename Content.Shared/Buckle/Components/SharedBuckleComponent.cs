using System;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Buckle.Components
{
    [NetworkedComponent()]
    public abstract class SharedBuckleComponent : Component, IDraggable
    {
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

        public bool DontCollide { get; set; }

        public abstract bool TryBuckle(EntityUid user, EntityUid to);

        bool IDraggable.CanDrop(CanDropEvent args)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<SharedStrapComponent>(args.Target);
        }

        bool IDraggable.Drop(DragDropEvent args)
        {
            return TryBuckle(args.User, args.Target);
        }
    }

    [Serializable, NetSerializable]
    public sealed class BuckleComponentState : ComponentState
    {
        public BuckleComponentState(bool buckled, int? drawDepth, EntityUid? lastEntityBuckledTo, bool dontCollide)
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
#pragma warning disable 618
    public abstract class BuckleChangeMessage : ComponentMessage
#pragma warning restore 618
    {
        /// <summary>
        ///     Constructs a new instance of <see cref="BuckleChangeMessage"/>
        /// </summary>
        /// <param name="entity">The entity that had its buckling status changed</param>
        /// <param name="strap">The strap that the entity was buckled to or unbuckled from</param>
        /// <param name="buckled">True if the entity was buckled, false otherwise</param>
        protected BuckleChangeMessage(EntityUid entity, EntityUid strap, bool buckled)
        {
            Entity = entity;
            Strap = strap;
            Buckled = buckled;
        }

        /// <summary>
        ///     The entity that had its buckling status changed
        /// </summary>
        public EntityUid Entity { get; }

        /// <summary>
        ///     The strap that the entity was buckled to or unbuckled from
        /// </summary>
        public EntityUid Strap { get; }

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
        public BuckleMessage(EntityUid entity, EntityUid strap) : base(entity, strap, true)
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
        public UnbuckleMessage(EntityUid entity, EntityUid strap) : base(entity, strap, false)
        {
        }
    }
}
