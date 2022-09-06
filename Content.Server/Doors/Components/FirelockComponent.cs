using Content.Shared.Doors.Components;

namespace Content.Server.Doors.Components
{
    /// <summary>
    /// Companion component to <see cref="DoorComponent"/> that handles firelock-specific behavior, including
    /// auto-closing on depressurization, air/fire alarm interactions, and preventing normal door functions when
    /// retaining pressure..
    /// </summary>
    [RegisterComponent]
    public sealed class FirelockComponent : Component
    {
        /// <summary>
        /// Pry time modifier to be used when the firelock is currently closed due to fire or pressure.
        /// </summary>
        /// <returns></returns>
        [DataField("lockedPryTimeModifier"), ViewVariables(VVAccess.ReadWrite)]
        public float LockedPryTimeModifier = 1.5f;

        [DataField("autocloseDelay")] public TimeSpan AutocloseDelay = TimeSpan.FromSeconds(3f);

        /// <summary>
        /// Maximum pressure difference before the firelock will refuse to open, in kPa.
        /// </summary>
        [DataField("pressureThreshold"), ViewVariables(VVAccess.ReadWrite)]
        public float PressureThreshold = 20;

        /// <summary>
        /// If true, and if this door has an <see cref="AtmosAlarmableComponent"/>, then it will only auto-close if the
        /// alarm is set to danger.
        /// </summary>
        [DataField("alarmAutoClose"), ViewVariables(VVAccess.ReadWrite)]
        public bool AlarmAutoClose = true;
    }
}
