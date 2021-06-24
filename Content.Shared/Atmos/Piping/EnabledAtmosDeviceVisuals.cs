using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping
{
    [Serializable, NetSerializable]
    public enum OutletInjectorVisuals
    {
        Enabled,
    }

    [Serializable, NetSerializable]
    public enum PassiveVentVisuals
    {
        Enabled,
    }

    [Serializable, NetSerializable]
    public enum VentScrubberVisuals
    {
        Enabled,
    }

    [Serializable, NetSerializable]
    public enum ThermoMachineVisuals
    {
        Enabled,
    }

    [Serializable, NetSerializable]
    public enum PressurePumpVisuals
    {
        Enabled,
    }
}
