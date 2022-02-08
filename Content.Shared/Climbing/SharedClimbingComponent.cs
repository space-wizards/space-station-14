using System;
using Content.Shared.ActionBlocker;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Climbing
{
    [NetworkedComponent()]
    public abstract class SharedClimbingComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        protected bool IsOnClimbableThisFrame
        {
            get
            {
                if (!_entMan.TryGetComponent<PhysicsComponent>(Owner, out var physicsComponent)) return false;

                foreach (var entity in physicsComponent.GetBodiesIntersecting())
                {
                    if ((entity.CollisionLayer & (int) CollisionGroup.SmallImpassable) != 0) return true;
                }

                return false;
            }
        }

        [ViewVariables]
        public virtual bool OwnerIsTransitioning
        {
            get => _ownerIsTransitioning;
            set
            {
                if (_ownerIsTransitioning == value) return;
                _ownerIsTransitioning = value;
                if (!_entMan.TryGetComponent<PhysicsComponent>(Owner, out var physicsComponent)) return;
                if (value)
                {
                    physicsComponent.BodyType = BodyType.Dynamic;
                }
                else
                {
                    physicsComponent.BodyType = BodyType.KinematicController;
                }
            }
        }

        private bool _ownerIsTransitioning = false;

        protected TimeSpan StartClimbTime = TimeSpan.Zero;

        /// <summary>
        ///     We'll launch the mob onto the table and give them at least this amount of time to be on it.
        /// </summary>
        public const float BufferTime = 0.3f;

        public virtual bool IsClimbing
        {
            get => _isClimbing;
            set
            {
                if (_isClimbing == value) return;
                _isClimbing = value;

                ToggleSmallPassable(value);
            }
        }

        private bool _isClimbing;

        // TODO: Layers need a re-work
        private void ToggleSmallPassable(bool value)
        {
            // Hope the mob has one fixture
            if (!_entMan.TryGetComponent<FixturesComponent>(Owner, out var fixturesComponent) || fixturesComponent.Deleted) return;

            foreach (var fixture in fixturesComponent.Fixtures.Values)
            {
                if (value)
                {
                    fixture.CollisionMask &= ~(int) CollisionGroup.SmallImpassable;
                }
                else
                {
                    fixture.CollisionMask |= (int) CollisionGroup.SmallImpassable;
                }
            }
        }

        [Serializable, NetSerializable]
        protected sealed class ClimbModeComponentState : ComponentState
        {
            public ClimbModeComponentState(bool climbing, bool isTransitioning)
            {
                Climbing = climbing;
                IsTransitioning = isTransitioning;
            }

            public bool Climbing { get; }
            public bool IsTransitioning { get; }
        }
    }
}
