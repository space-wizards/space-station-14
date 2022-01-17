using System;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Climbing.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClimbingComponent))]
    public class ClimbingComponent : SharedClimbingComponent
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override bool IsClimbing
        {
            get => base.IsClimbing;
            set
            {
                if (_isClimbing == value)
                    return;

                base.IsClimbing = value;

                if (value)
                {
                    StartClimbTime = IoCManager.Resolve<IGameTiming>().CurTime;
                    EntitySystem.Get<ClimbSystem>().AddActiveClimber(this);
                    OwnerIsTransitioning = true;
                }
                else
                {
                    EntitySystem.Get<ClimbSystem>().RemoveActiveClimber(this);
                    OwnerIsTransitioning = false;
                }

                Dirty();
            }
        }

        public override bool OwnerIsTransitioning
        {
            get => base.OwnerIsTransitioning;
            set
            {
                if (value == base.OwnerIsTransitioning) return;
                base.OwnerIsTransitioning = value;
                Dirty();
            }
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case BuckleMessage msg:
                    if (msg.Buckled)
                        IsClimbing = false;

                    break;
            }
        }

        /// <summary>
        /// Make the owner climb from one point to another
        /// </summary>
        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (!_entityManager.TryGetComponent<PhysicsComponent>(Owner, out var physicsComponent)) return;

            var velocity = (to - from).Length;

            if (velocity <= 0.0f) return;

            // Since there are bodies with different masses:
            // mass * 5 seems enough to move entity
            // instead of launching cats like rockets against the walls with constant impulse value.
            physicsComponent.ApplyLinearImpulse((to - from).Normalized * velocity * physicsComponent.Mass * 5);
            OwnerIsTransitioning = true;

            EntitySystem.Get<ClimbSystem>().UnsetTransitionBoolAfterBufferTime(Owner, this);
        }

        public void Update()
        {
            if (!IsClimbing || _gameTiming.CurTime < TimeSpan.FromSeconds(BufferTime) + StartClimbTime)
            {
                return;
            }

            if (!IsOnClimbableThisFrame && IsClimbing)
                IsClimbing = false;
        }

        public override ComponentState GetComponentState()
        {
            return new ClimbModeComponentState(_isClimbing, OwnerIsTransitioning);
        }
    }
}
