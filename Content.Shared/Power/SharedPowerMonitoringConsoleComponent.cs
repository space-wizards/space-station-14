using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public PowerMonitoringConsoleEntry[] Loads;
    public NetCoordinates[][] HVCables;
    public NetCoordinates[][] MVCables;
    public NetCoordinates[][] LVCables;
    public bool Snap;
    public float Precision;

    public PowerMonitoringConsoleBoundInterfaceState(PowerMonitoringConsoleEntry[] loads,
        NetCoordinates[][] hvCables,
        NetCoordinates[][] mvCables,
        NetCoordinates[][] lvCables,
        bool snap,
        float precision)
    {
        Loads = loads;
        HVCables = hvCables;
        MVCables = mvCables;
        LVCables = lvCables;
        Snap = snap;
        Precision = precision;
    }
}

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleEntry
{
    public NetEntity NetEntity;
    public NetCoordinates Coordinates;
    public string NameLocalized;
    public string IconEntityPrototypeId;
    public double Size;
    public bool IsBattery;

    public PowerMonitoringConsoleEntry(NetEntity netEntity, NetCoordinates coordinates, string nl, string ipi, double size, bool isBattery)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        NameLocalized = nl;
        IconEntityPrototypeId = ipi;
        Size = size;
        IsBattery = isBattery;
    }
}

[Serializable, NetSerializable]
public enum PowerMonitoringConsoleUiKey
{
    Key
}

