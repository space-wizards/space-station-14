using Content.Shared.Power.Pow3r;

namespace Content.Shared.Power.NodeGroups;

public partial struct PowerNet : IPowerNetwork
{
    /*public override string? GetDebugData()
    {
        if (PowerNetSystem == null)
            return null;

        // This is just recycling the multi-tool examine.
        var ps = PowerNetSystem.GetNetworkStatistics(NetworkNode);

        float storageRatio = ps.InStorageCurrent / Math.Max(ps.InStorageMax, 1.0f);
        float outStorageRatio = ps.OutStorageCurrent / Math.Max(ps.OutStorageMax, 1.0f);
        return @$"Current Supply: {ps.SupplyCurrent:G3}
From Batteries: {ps.SupplyBatteries:G3}
Theoretical Supply: {ps.SupplyTheoretical:G3}
Ideal Consumption: {ps.Consumption:G3}
Input Storage: {ps.InStorageCurrent:G3} / {ps.InStorageMax:G3} ({storageRatio:P1})
Output Storage: {ps.OutStorageCurrent:G3} / {ps.OutStorageMax:G3} ({outStorageRatio:P1})";
    }*/

    [ViewVariables]
    public NodeId Id { get; set; }

    [ViewVariables]
    public List<NodeId> Supplies { get; set; } = new();

    [ViewVariables]
    public List<NodeId> Loads { get; set; } = new();

    [ViewVariables]
    public List<NodeId> BatteryLoads { get; set; } = new();

    [ViewVariables]
    public List<NodeId> BatterySupplies { get; set; } = new();

    [ViewVariables]
    public float LastCombinedLoad { get; set; }

    [ViewVariables]
    public float LastCombinedSupply { get; set; }

    [ViewVariables]
    public float LastCombinedMaxSupply { get; set; }

    [ViewVariables]
    public int Height { get; set; }
}
