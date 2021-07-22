using System;
using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Piping.Components
{
    /// <summary>
    ///     Adds itself to a <see cref="IGridAtmosphereComponent"/> to be updated by.
    /// </summary>
    [RegisterComponent]
    public class AtmosDeviceComponent : Component
    {
        public override string Name => "AtmosDevice";

        /// <summary>
        ///     Whether this device requires being anchored to join an atmosphere.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requireAnchored")]
        public bool RequireAnchored { get; private set; } = true;

        [ViewVariables]
        public TimeSpan LastProcess { get; set; } = TimeSpan.Zero;

        public GridId? JoinedGrid { get; set; }
    }

    public sealed class AtmosDeviceUpdateEvent : EntityEventArgs
    {}

    public sealed class AtmosDeviceEnabledEvent : EntityEventArgs
    {}

    public sealed class AtmosDeviceDisabledEvent : EntityEventArgs
    {}
}
