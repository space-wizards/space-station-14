using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics.Joints;
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

        public float? MaxDistance => PullJoint?.MaxLength;

        /// <summary>
        /// The current entity pulling this component.
        /// SharedPullingStateManagementSystem should be writing this. This means definitely not you.
        /// </summary>
        public EntityUid? Puller { get; set; }
        /// <summary>
        /// The pull joint.
        /// SharedPullingStateManagementSystem should be writing this. This means probably not you.
        /// </summary>
        public DistanceJoint? PullJoint { get; set; }

        public bool BeingPulled => Puller != default;

        public EntityCoordinates? MovingTo { get; set; }

        public override ComponentState GetComponentState()
        {
            return new PullableComponentState(Puller);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not PullableComponentState state)
            {
                return;
            }

            if (!state.Puller.HasValue)
            {
                EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPullable(this);
                return;
            }

            if (!state.Puller.Value.IsValid())
            {
                Logger.Error($"Invalid entity {state.Puller.Value} for pulling");
                return;
            }

            if (Puller == state.Puller)
            {
                // don't disconnect and reconnect a puller for no reason
                return;
            }

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<SharedPullerComponent?>(state.Puller.Value, out var comp))
            {
                Logger.Error($"Entity {state.Puller.Value} for pulling had no Puller component");
                // ensure it disconnects from any different puller, still
                EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPullable(this);
                return;
            }

            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceRelationship(comp, this);
        }

        protected override void Shutdown()
        {
            EntitySystem.Get<SharedPullingStateManagementSystem>().ForceDisconnectPullable(this);
            base.Shutdown();
        }

        protected override void OnRemove()
        {
            if (Puller != default)
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
