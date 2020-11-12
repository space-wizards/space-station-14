using System;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Content.Shared.Interfaces.GameObjects.Components;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedClimbingComponent : Component, IActionBlocker, ICollideSpecial, IDraggable
    {
        public sealed override string Name => "Climbing";
        public sealed override uint? NetID => ContentNetIDs.CLIMBING;

        protected IPhysicsComponent Body;
        protected bool IsOnClimbableThisFrame = false;

        protected bool OwnerIsTransitioning { get; set; }
        public abstract bool IsClimbing { get; set; }

        bool IActionBlocker.CanMove() => !OwnerIsTransitioning;
        bool IActionBlocker.CanChangeDirection() => !OwnerIsTransitioning;

        bool ICollideSpecial.PreventCollide(IPhysBody collided)
        {
            if ((collided.CollisionLayer & (int) CollisionGroup.VaultImpassable) != 0 && collided.Entity.HasComponent<SharedClimbableComponent>())
            {
                IsOnClimbableThisFrame = true;
                return IsClimbing;
            }

            return false;
        }

        bool IDraggable.CanDrop(CanDropEventArgs args)
        {
            return args.Target.HasComponent<SharedClimbableComponent>();
        }

        bool IDraggable.Drop(DragDropEventArgs args)
        {
            return false;
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out Body);
        }

        [Serializable, NetSerializable]
        protected sealed class ClimbModeComponentState : ComponentState
        {
            public ClimbModeComponentState(bool climbing) : base(ContentNetIDs.CLIMBING)
            {
                Climbing = climbing;
            }

            public bool Climbing { get; }
        }
    }
}
