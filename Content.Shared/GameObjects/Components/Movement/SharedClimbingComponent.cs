using System;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedClimbingComponent : Component, IActionBlocker, ICollideSpecial
    {
        public sealed override string Name => "Climbing";
        public sealed override uint? NetID => ContentNetIDs.CLIMBING;

        protected IPhysicsComponent Body;
        protected bool IsOnClimbableThisFrame = false;

        protected bool OwnerIsTransitioning
        {
            get
            {
                if (Body.TryGetController<ClimbController>(out var controller))
                {
                    return controller.IsActive;
                }

                return false;
            }
        }

        public abstract bool IsClimbing { get; set; }

        bool IActionBlocker.CanMove() => !OwnerIsTransitioning;
        bool IActionBlocker.CanChangeDirection() => !OwnerIsTransitioning;

        bool ICollideSpecial.PreventCollide(IPhysBody collided)
        {
            if (((CollisionGroup)collided.CollisionLayer).HasFlag(CollisionGroup.VaultImpassable) && collided.Entity.HasComponent<IClimbable>())
            {
                IsOnClimbableThisFrame = true;
                return IsClimbing;
            }

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
