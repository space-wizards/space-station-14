using Content.Shared.Collections;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power.Pow3r;

namespace Content.Shared.Power.NodeGroups;

public sealed class PowerNet : BaseNodeGroup, IPowerNetwork
{
    [ViewVariables]
    public readonly HashSet<EntityUid> Chargers = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Dischargers = new();

    [ViewVariables]
    public HashSet<EntityUid> Consumers { get; set; } = new();

    [ViewVariables]
    public HashSet<EntityUid> Suppliers { get; set; } = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Apcs = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Providers = new();

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
