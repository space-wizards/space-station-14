#nullable enable
using System;
using Content.Server.Atmos.Components;
using Robust.Shared.GameObjects;
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

        public IGridAtmosphereComponent? Atmosphere { get; set; }

        [ViewVariables]
        public TimeSpan LastProcess { get; set; } = TimeSpan.Zero;
    }

    public abstract class BaseAtmosDeviceEvent : EntityEventArgs
    {
        public IGridAtmosphereComponent Atmosphere { get; }

        public BaseAtmosDeviceEvent(IGridAtmosphereComponent atmosphere)
        {
            Atmosphere = atmosphere;
        }
    }

    public sealed class AtmosDeviceUpdateEvent : BaseAtmosDeviceEvent
    {
        public AtmosDeviceUpdateEvent(IGridAtmosphereComponent atmosphere) : base(atmosphere)
        {
        }
    }

    public sealed class AtmosDeviceEnabledEvent : BaseAtmosDeviceEvent
    {
        public AtmosDeviceEnabledEvent(IGridAtmosphereComponent atmosphere) : base(atmosphere)
        {
        }
    }

    public sealed class AtmosDeviceDisabledEvent : BaseAtmosDeviceEvent
    {
        public AtmosDeviceDisabledEvent(IGridAtmosphereComponent atmosphere) : base(atmosphere)
        {
        }
    }
}
