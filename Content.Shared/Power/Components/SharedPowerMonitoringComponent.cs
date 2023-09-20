using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringBoundInterfaceState : BoundUserInterfaceState
{
    public int Power;
    public ExternalPowerState ExternalPower;
    public float Charge;
    public double TotalSources;
    public double TotalLoads;
    public PowerMonitoringEntry[] Sources;
    public PowerMonitoringEntry[] Loads;

    public PowerMonitoringBoundInterfaceState(int power,
        ExternalPowerState externalPower,
        float charge,
        double totalSources,
        double totalLoads,
        PowerMonitoringEntry[] sources,
        PowerMonitoringEntry[] loads)
    {
        Power = power;
        ExternalPower = externalPower;
        Charge = charge;
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        Sources = sources;
        Loads = loads;
    }
}

[Serializable, NetSerializable]
public sealed class PowerMonitoringEntry
{
    public NetEntity NetEntity;
    public string NameLocalized;
    public string IconEntityPrototypeId;
    public double Size;
    public bool IsBattery;

    public PowerMonitoringEntry(NetEntity netEntity, string nl, string ipi, double size, bool isBattery)
    {
        NetEntity = netEntity;
        NameLocalized = nl;
        IconEntityPrototypeId = ipi;
        Size = size;
        IsBattery = isBattery;
    }
}

[Serializable, NetSerializable]
public enum ExternalPowerState
{
    None,
    Low,
    Stable,
    Good,
}

[Serializable, NetSerializable]
public enum PowerMonitoringDistributorUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PowerMonitoringConsoleUiKey
{
    Key
}

