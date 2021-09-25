using System;
using Content.Shared.Physics.Pull;
using Robust.Shared.Analyzers;
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

        public float? MaxDistance => PullJoint?.MaxLength;

        private MapCoordinates? _movingTo;

        // Temporary until Friend can be applied (it applies to methods, so.)
        public void UpdatePullerFromSharedPullingStateManagementSystem(IEntity? v) { Puller = v; }

        /// <summary>
        /// The current entity pulling this component.
        /// Ideally, alter using TryStartPull and TryStopPull.
        /// </summary>
        public IEntity? Puller { get; private set; }
        /// <summary>
        /// The pull joint.
        /// SharedPullingStateManagementSystem should be writing this. This means probably not you.
        /// </summary>
        public DistanceJoint? PullJoint { get; set; }

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
            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPullable(this);
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
