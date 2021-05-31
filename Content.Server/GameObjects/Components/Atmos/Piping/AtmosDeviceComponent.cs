#nullable enable
using Content.Server.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos.Piping
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
    }

    public class AtmosDeviceUpdateEvent : EntityEventArgs
    {
        public IGridAtmosphereComponent Atmosphere { get; }

        public AtmosDeviceUpdateEvent(IGridAtmosphereComponent atmosphere)
        {
            Atmosphere = atmosphere;
        }
    }
}
