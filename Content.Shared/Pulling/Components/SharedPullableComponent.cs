using System;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Pulling.Components
{
    [NetworkedComponent()]
    public abstract class SharedPullableComponent : Component
    {
        public override string Name => "Pullable";

        [ComponentDependency] private readonly PhysicsComponent? _physics = default!;

        public float? MaxDistance => _pullJoint?.MaxLength;

        private MapCoordinates? _movingTo;

        // This isn't cleanly cut off yet because Puller gets set and that is supposed to trigger state stuff.
        // So right now it keeps the "private" name as things get shuffled to a more ECSy approach.
        // FRIENDZONE THESE W/ SHAREDPULLINGSYSTEM, OR EVEN MORE FINE-GRAINED THAN THAT
        public IEntity? _puller;
        public DistanceJoint? _pullJoint;

        /// <summary>
        /// The current entity pulling this component.
        /// Setting this performs the entire setup process for pulling.
        /// </summary>
        public virtual IEntity? Puller
        {
            get => _puller;
            set
            {
                // Bit of a disparity here, because of how TryStopPull is handled
                if (value == null)
                {
                    SharedPullingStateManagementSystem.ForceRelationship(null, this);
                }
                else
                {
                    SharedPullingStateManagementSystem.StartPulling(value.GetComponent<SharedPullerComponent>(), this);
                }
            }
        }

        public bool BeingPulled => Puller != null;

        public MapCoordinates? MovingTo
        {
            get => _movingTo;
            set
            {
                if (_movingTo == value)
                {
                    return;
                }

                _movingTo = value;

                if (value == null)
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PullableStopMovingMessage());
                }
                else
                {
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new PullableMoveMessage());
                }
            }
        }

        public bool TryStartPull(IEntity puller)
        {
            if (!EntitySystem.Get<SharedPullingSystem>().CanPull(puller, Owner))
            {
                return false;
            }

            TryStopPull();

            Puller = puller;

            if(Puller != puller)
            {
                return false;
            }

            return true;
        }

        public bool TryStopPull(IEntity? user = null)
        {
            if (!BeingPulled)
            {
                return false;
            }

            var msg = new StopPullingEvent(user?.Uid);
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, msg);

            if (msg.Cancelled) return false;

            if (user != null && user.TryGetComponent<SharedPullerComponent>(out var puller))
            {
                puller.Pulling = null;
            }

            Puller = null;
            return true;
        }

        public bool TogglePull(IEntity puller)
        {
            if (BeingPulled)
            {
                if (Puller == puller)
                {
                    return TryStopPull();
                }
                else
                {
                    TryStopPull();
                    return TryStartPull(puller);
                }
            }

            return TryStartPull(puller);
        }

        public bool TryMoveTo(MapCoordinates to)
        {
            if (Puller == null)
            {
                return false;
            }

            if (_physics == null)
            {
                return false;
            }

            MovingTo = to;
            return true;
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PullableComponentState(Puller?.Uid);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not PullableComponentState state)
            {
                return;
            }

            if (state.Puller == null)
            {
                Puller = null;
                return;
            }

            if (!Owner.EntityManager.TryGetEntity(state.Puller.Value, out var entity))
            {
                Logger.Error($"Invalid entity {state.Puller.Value} for pulling");
                return;
            }

            Puller = entity;
        }

        protected override void OnRemove()
        {
            TryStopPull();
            MovingTo = null;

            base.OnRemove();
        }
    }

    [Serializable, NetSerializable]
    public class PullableComponentState : ComponentState
    {
        public readonly EntityUid? Puller;

        public PullableComponentState(EntityUid? puller)
        {
            Puller = puller;
        }
    }

    /// <summary>
    /// Raised when a request is made to stop pulling an entity.
    /// </summary>
    public sealed class StopPullingEvent : CancellableEntityEventArgs
    {
        public EntityUid? User { get; }

        public StopPullingEvent(EntityUid? uid = null)
        {
            User = uid;
        }
    }
}
