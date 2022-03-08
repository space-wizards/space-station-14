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

    public sealed class BuckleChangeEvent : EntityEventArgs
    {
        public EntityUid Strap;
        public bool Buckling;
    }

    [Serializable, NetSerializable]
    public enum BuckleVisuals
    {
        Buckled
    }
}
