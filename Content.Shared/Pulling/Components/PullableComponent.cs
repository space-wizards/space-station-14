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
    // Before you try to add another type than SharedPullingStateManagementSystem, consider the can of worms you may be opening!
    [NetworkedComponent()]
    [Friend(typeof(SharedPullingStateManagementSystem))]
    [RegisterComponent]
    public class SharedPullableComponent : Component
    {
        public override string Name => "Pullable";

        // At this point this field exists solely for the component dependency (which is mandatory).
        [ComponentDependency] private readonly PhysicsComponent? _physics = default!;

        public float? MaxDistance => PullJoint?.MaxLength;

        /// <summary>
        /// The current entity pulling this component.
        /// Ideally, alter using TryStartPull and TryStopPull.
        /// </summary>
        public IEntity? Puller { get; set; }
        /// <summary>
        /// The pull joint.
        /// SharedPullingStateManagementSystem should be writing this. This means probably not you.
        /// </summary>
        public DistanceJoint? PullJoint { get; set; }

        public bool BeingPulled => Puller != null;

        public MapCoordinates? MovingTo { get; set; }

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

        protected override void Shutdown()
        {
            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPullable(this);
            base.Shutdown();
        }

        protected override void OnRemove()
        {
            if (Puller != null)
            {
                // This is absolute paranoia but it's also absolutely necessary. Too many puller state bugs. - 20kdc
                Logger.ErrorS("c.go.c.pulling", "PULLING STATE CORRUPTION IMMINENT IN PULLABLE {0} - OnRemove called when Puller is set!", Owner);
            }
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
