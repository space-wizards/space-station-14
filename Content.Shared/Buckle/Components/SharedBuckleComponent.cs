using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Standing;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components
{
    [NetworkedComponent()]
    public abstract class SharedBuckleComponent : Component, IDraggable
    {
        [Dependency] protected readonly IEntityManager EntMan = default!;

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

        /// <summary>
        ///     Reattaches this entity to the strap, modifying its position and rotation.
        /// </summary>
        /// <param name="strap">The strap to reattach to.</param>
        public void ReAttach(SharedStrapComponent strap)
        {
            var ownTransform = EntMan.GetComponent<TransformComponent>(Owner);
            var strapTransform = EntMan.GetComponent<TransformComponent>(strap.Owner);

            ownTransform.AttachParent(strapTransform);
            ownTransform.LocalRotation = Angle.Zero;

            switch (strap.Position)
            {
                case StrapPosition.None:
                    break;
                case StrapPosition.Stand:
                    EntitySystem.Get<StandingStateSystem>().Stand(Owner);
                    break;
                case StrapPosition.Down:
                    EntitySystem.Get<StandingStateSystem>().Down(Owner, false, false);
                    break;
            }

            ownTransform.LocalPosition = strap.BuckleOffset;
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

        public EntityUid BuckledEntity;
        public bool Buckling;
    }

    [Serializable, NetSerializable]
    public enum BuckleVisuals
    {
        Buckled
    }
}
