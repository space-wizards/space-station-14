namespace Content.Server.Atmos.Piping.Components
{
    /// <summary>
    ///     Adds itself to a <see cref="IAtmosphereComponent"/> to be updated by.
    /// </summary>
    [RegisterComponent]
    public sealed partial class AtmosDeviceComponent : Component
    {
        /// <summary>
        ///     If true, this device must be anchored before it will receive any AtmosDeviceUpdateEvents.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; private set; } = true;

        /// <summary>
        ///     If true, update even when there is no grid atmosphere. Normally, atmos devices only
        ///     update when inside a grid atmosphere, because they work with gases in the environment
        ///     and won't do anything useful if there is no environment. This is useful for devices
        ///     like gas canisters whose contents can still react if the canister itself is not inside
        ///     a grid atmosphere.
        /// </summary>
        [DataField("joinSystem")]
        public bool JoinSystem { get; private set; } = false;

        /// <summary>
        ///     If non-null, the grid that this device is part of.
        /// </summary>
        public EntityUid? JoinedGrid { get; set; }

        /// <summary>
        ///     Indicates that a device is not on a grid atmosphere but still being updated.
        /// </summary>
        [ViewVariables]
        public bool JoinedSystem { get; set; } = false;

        [ViewVariables]
        public TimeSpan LastProcess { get; set; } = TimeSpan.Zero;
    }

    public sealed class AtmosDeviceUpdateEvent : EntityEventArgs
    {
        /// <summary>
        /// Time elapsed since last update, in seconds. Multiply values used in the update handler
        /// by this number to make them tickrate-invariant. Use this number instead of AtmosphereSystem.AtmosTime.
        /// </summary>
        public float dt;

        public AtmosDeviceUpdateEvent(float dt)
        {
            this.dt = dt;
        }
    }

    public sealed class AtmosDeviceEnabledEvent : EntityEventArgs
    {}

    public sealed class AtmosDeviceDisabledEvent : EntityEventArgs
    {}
}
