using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Atmos
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
}
