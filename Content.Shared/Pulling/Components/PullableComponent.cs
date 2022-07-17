using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Serialization;

namespace Content.Shared.Pulling.Components
{
    // Before you try to add another type than SharedPullingStateManagementSystem, consider the can of worms you may be opening!
    [NetworkedComponent()]
    [Access(typeof(SharedPullingStateManagementSystem))]
    [RegisterComponent]
    public sealed class SharedPullableComponent : Component
    {
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

        public bool BeingPulled => Puller != null;

        [Access(typeof(SharedPullingStateManagementSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public EntityCoordinates? MovingTo { get; set; }

        /// <summary>
        /// If the physics component has FixedRotation should we keep it upon being pulled
        /// </summary>
        [Access(typeof(SharedPullingSystem), Other = AccessPermissions.ReadExecute)]
        [ViewVariables(VVAccess.ReadWrite), DataField("fixedRotation")]
        public bool FixedRotationOnPull { get; set; }

        /// <summary>
        /// What the pullable's fixedrotation was set to before being pulled.
        /// </summary>
        [Access(typeof(SharedPullingSystem), Other = AccessPermissions.ReadExecute)]
        [ViewVariables]
        public bool PrevFixedRotation;

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
    public sealed class PullableComponentState : ComponentState
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
