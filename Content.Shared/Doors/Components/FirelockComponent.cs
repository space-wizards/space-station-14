using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components
{
    /// <summary>
    /// Companion component to <see cref="DoorComponent"/> that handles firelock-specific behavior, including
    /// auto-closing on air/fire alarm, and preventing normal door functions when
    /// retaining pressure..
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class FirelockComponent : Component
    {
        #region Settings

        /// <summary>
        /// Pry time modifier to be used when the firelock is currently closed due to fire or pressure.
        /// </summary>
        /// <returns></returns>
        [DataField("lockedPryTimeModifier"), ViewVariables(VVAccess.ReadWrite)]
        public float LockedPryTimeModifier = 1.5f;

        /// <summary>
        /// The cooldown duration before a firelock can automatically close due to a hazardous environment after it has
        /// been pried open. Measured in seconds.
        /// </summary>
        [DataField]
        public TimeSpan EmergencyCloseCooldownDuration = TimeSpan.FromSeconds(2);

        #endregion

        #region Set by system

        /// <summary>
        /// When the firelock will be allowed to automatically close again due to a hazardous environment.
        /// </summary>
        [DataField]
        public TimeSpan? EmergencyCloseCooldown;

        /// <summary>
        /// Whether the firelock can open, or is locked due to its environment.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool IsLocked;

        /// <summary>
        /// Whether the airlock is powered.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Powered;

        #endregion
    }
}
