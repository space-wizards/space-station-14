#nullable enable
using Robust.Shared.Serialization;

namespace Content.Shared.Power;

[Serializable, NetSerializable]
public sealed class PowerDistributorBoundInterfaceState : BoundUserInterfaceState
{
    public int Power;
    public PowerDistributorExternalPowerState ExternalPower;
    public float Charge;
    public double TotalSources;
    public double TotalLoads;
    public PowerDistributorEntry[] Sources;
    public PowerDistributorEntry[] Loads;

    public PowerDistributorBoundInterfaceState(int power,
        PowerDistributorExternalPowerState externalPower,
        float charge,
        double totalSources,
        double totalLoads,
        PowerDistributorEntry[] sources,
        PowerDistributorEntry[] loads)
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
public sealed class PowerDistributorEntry
{
    public string NameLocalized;
    public string IconEntityPrototypeId;
    public double Size;
    public bool IsBattery;
    public PowerDistributorEntry(string nl, string ipi, double size, bool isBattery)
    {
        NameLocalized = nl;
        IconEntityPrototypeId = ipi;
        Size = size;
        IsBattery = isBattery;
    }
}

public enum PowerDistributorExternalPowerState
{
    None,
    Low,
    Good,
}

[Serializable, NetSerializable]
public enum PowerDistributorUiKey
{
    Key
}

