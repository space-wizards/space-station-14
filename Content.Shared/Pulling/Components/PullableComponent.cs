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
    public sealed partial class SharedPullableComponent : Component
    {
        /// <summary>
        /// The current entity pulling this component.
        /// </summary>
        public EntityUid? Puller { get; set; }

        /// <summary>
        /// The pull joint.
        /// </summary>
        public string? PullJointId { get; set; }

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
        public readonly NetEntity? Puller;

        public PullableComponentState(NetEntity? puller)
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
