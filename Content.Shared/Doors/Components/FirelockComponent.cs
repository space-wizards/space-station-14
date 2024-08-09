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
        /// Maximum pressure difference before the firelock will refuse to open, in kPa.
        /// </summary>
        [DataField("pressureThreshold"), ViewVariables(VVAccess.ReadWrite)]
        public float PressureThreshold = 80;

        /// <summary>
        /// Maximum temperature difference before the firelock will refuse to open, in k.
        /// </summary>
        [DataField("temperatureThreshold"), ViewVariables(VVAccess.ReadWrite)]
        public float TemperatureThreshold = 330;
        // this used to check for hot-spots, but because accessing that data is a a mess this now just checks
        // temperature. This does mean a cold room will trigger hot-air pop-ups

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
        /// Whether or not a pressure difference exceeding the pressure threshold exists around the firelock.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Pressure;

        /// <summary>
        /// Whether or not the airlock is commanded by an air alarm to close.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool ExtLocked;

        /// <summary>
        /// Whether the firelock can open, or is locked due to its environment. Note that even when locked,
        /// the firelock can still be pried, so this should be more accurately named "WantsToClose".
        /// </summary>
        public bool IsLocked => ExtLocked || Pressure;

        /// <summary>
        /// Whether the airlock is powered.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Powered;

        #endregion
    }
}
