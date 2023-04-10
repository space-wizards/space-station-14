namespace Content.Server.Atmos.Piping.Components
{
    /// <summary>
    ///     Adds itself to a <see cref="IAtmosphereComponent"/> to be updated by.
    /// </summary>
    [RegisterComponent]
    public sealed class AtmosDeviceComponent : Component
    {
        /// <summary>
        ///     Whether this device requires being anchored to join an atmosphere.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; private set; } = true;

        /// <summary>
        ///     Whether this device will join an entity system to process when not in a grid.
        /// </summary>
        [DataField("joinSystem")]
        public bool JoinSystem { get; } = false;

        /// <summary>
        ///     Whether we have joined an entity system to process.
        /// </summary>
        [ViewVariables]
        public bool JoinedSystem { get; set; } = false;

        [ViewVariables]
        public TimeSpan LastProcess { get; set; } = TimeSpan.Zero;

        public EntityUid? JoinedGrid { get; set; }
    }

    public sealed class AtmosDeviceUpdateEvent : EntityEventArgs
    {}

    public sealed class AtmosDeviceEnabledEvent : EntityEventArgs
    {}

    public sealed class AtmosDeviceDisabledEvent : EntityEventArgs
    {}
}
