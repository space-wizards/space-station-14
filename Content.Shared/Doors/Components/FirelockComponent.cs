using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components
{
    /// <summary>
    /// Companion component to <see cref="DoorComponent"/> that handles alarm behavior, responding to alarms that lock
    /// and unlock the door.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class FirelockComponent : Component
    {
        #region Settings

        /// <summary>
        /// Pry time modifier to be used when the alarm is currently triggered.
        /// </summary>
        /// <returns></returns>
        [DataField]
        public float LockedPryTimeModifier = 1.5f;

        /// <summary>
        /// The cooldown duration before an alarmed door can automatically close due to a hazardous environment after it
        /// has been pried open. Measured in seconds.
        /// </summary>
        [DataField]
        public TimeSpan EmergencyCloseCooldownDuration = TimeSpan.FromSeconds(2);

        #endregion

        #region Set by system

        /// <summary>
        /// When the firelock will be allowed to automatically close again due to an alarm.
        /// </summary>
        [DataField]
        public TimeSpan? EmergencyCloseCooldown;

        /// <summary>
        /// Whether the alarm is currently active.
        /// </summary>
        public bool IsActive => IsTriggered && IsPowered;

        /// <summary>
        /// Whether the alarm has been triggered.
        /// </summary>#
        [DataField, AutoNetworkedField]
        public bool IsTriggered;

        /// <summary>
        /// Whether the alarm itself has power.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool IsPowered;

        public HashSet<EntityUid> AlarmSources = [];

        #endregion
    }
}
