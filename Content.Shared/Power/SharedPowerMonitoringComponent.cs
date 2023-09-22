using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerMonitoringBoundInterfaceState : BoundUserInterfaceState
{
    public double TotalSources;
    public double TotalLoads;
    public PowerMonitoringEntry[] Sources;
    public PowerMonitoringEntry[] Loads;
    public float Charge;
    public ExternalPowerState ExternalPower;

    public PowerMonitoringBoundInterfaceState(
        double totalSources,
        double totalLoads,
        PowerMonitoringEntry[] sources,
        PowerMonitoringEntry[] loads,
        float charge = 0f,
        ExternalPowerState externalPower = ExternalPowerState.None)
    {
        TotalSources = totalSources;
        TotalLoads = totalLoads;
        Sources = sources;
        Loads = loads;
        Charge = charge;
        ExternalPower = externalPower;
    }
}

[Serializable, NetSerializable]
public sealed class PowerMonitoringEntry
{
    public readonly NetEntity NetEntity;
    public readonly string NameLocalized;
    public readonly double Size;
    public readonly bool IsBattery;

    public PowerMonitoringEntry(NetEntity netEntity, string nameLocalized, double size, bool isBattery)
    {
        NetEntity = netEntity;
        NameLocalized = nameLocalized;
        Size = size;
        IsBattery = isBattery;
    }
}


[Serializable, NetSerializable]
public sealed class PowerMonitoringUIChangedMessage : BoundUserInterfaceMessage
{
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
public enum PowerMonitoringSMESUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PowerMonitoringSubstationUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PowerMonitoringUiKey
{
    Key
}

