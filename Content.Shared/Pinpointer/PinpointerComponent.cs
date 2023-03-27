using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer
{
    /// <summary>
    /// Displays a sprite on the item that points towards the target component.
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    [Access(typeof(SharedPinpointerSystem))]
    public sealed class PinpointerComponent : Component
    {
        // TODO: Type serializer oh god
        [DataField("component"), ViewVariables(VVAccess.ReadWrite)]
        public string? Component;

        [DataField("mediumDistance"), ViewVariables(VVAccess.ReadWrite)]
        public float MediumDistance = 16f;

        [DataField("closeDistance"), ViewVariables(VVAccess.ReadWrite)]
        public float CloseDistance = 8f;

        [DataField("reachedDistance"), ViewVariables(VVAccess.ReadWrite)]
        public float ReachedDistance = 1f;

        /// <summary>
        ///     Pinpointer arrow precision in radians.
        /// </summary>
        [DataField("precision"), ViewVariables(VVAccess.ReadWrite)]
        public double Precision = 0.09;

        /// <summary>
        ///     Name to display of the target being tracked.
        /// </summary>
        [DataField("targetName"), ViewVariables(VVAccess.ReadWrite)]
        public string? TargetName;

        /// <summary>
        ///     Whether or not the target name should be updated when the target is updated.
        /// </summary>
        [DataField("updateTargetName"), ViewVariables(VVAccess.ReadWrite)]
        public bool UpdateTargetName;

        /// <summary>
        ///     Whether or not the target can be reassigned.
        /// </summary>
        [DataField("canRetarget"), ViewVariables(VVAccess.ReadWrite)]
        public bool CanRetarget;

        [ViewVariables(VVAccess.ReadOnly)]
        public EntityUid? Target = null;

        public bool IsActive = false;
        public Angle ArrowAngle;
        public Distance DistanceToTarget = Distance.Unknown;
        public bool HasTarget => DistanceToTarget != Distance.Unknown;
    }

    [Serializable, NetSerializable]
    public sealed class PinpointerComponentState : ComponentState
    {
        public bool IsActive;
        public Angle ArrowAngle;
        public Distance DistanceToTarget;
    }

    [Serializable, NetSerializable]
    public enum Distance : byte
    {
        Unknown,
        Reached,
        Close,
        Medium,
        Far
    }
}
