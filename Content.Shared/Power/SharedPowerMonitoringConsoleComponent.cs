#nullable enable
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public double TotalSources;
    public double TotalLoads;
    public PowerMonitoringConsoleEntry[] Sources;
    public PowerMonitoringConsoleEntry[] Loads;
    public PowerMonitoringConsoleBoundInterfaceState(double totalSources, double totalLoads, PowerMonitoringConsoleEntry[] sources, PowerMonitoringConsoleEntry[] loads)
    {
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        Sources = sources;
        Loads = loads;
    }
}

[Serializable, NetSerializable]
public sealed class PowerMonitoringConsoleEntry
{
    public string NameLocalized;
    public string IconEntityPrototypeId;
    public double Size;
    public bool IsBattery;
    public PowerMonitoringConsoleEntry(string nl, string ipi, double size, bool isBattery)
    {
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

