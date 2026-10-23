using Content.Server.Power.NodeGroups;
using Content.Shared.Examine;
using Content.Shared.NodeContainer;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

/// <inheritdoc/>
public sealed partial class ServerCableMultitoolSystem : CableMultitoolSystem
{
    [Dependency] private PowerNetSystem _powerNet = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    protected override void ExamineCable(Entity<CableComponent> ent, EntityUid user)
    {
        var markup = FormattedMessage.FromMarkupOrThrow(GenerateCableMarkup((ent.Owner, null)));
        _examine.SendExamineTooltip(user, ent.Owner, markup, false, false);
    }

    private string GenerateCableMarkup(Entity<NodeContainerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return Loc.GetString("cable-multitool-system-internal-error-missing-component");

        foreach (var node in ent.Comp.Nodes)
        {
            if (node.Value.NodeGroup is not IBasePowerNet net)
                continue;

            var ps = _powerNet.GetNetworkStatistics(net.NetworkNode);

            var storageRatio = ps.InStorageCurrent / Math.Max(ps.InStorageMax, 1.0f);
            var outStorageRatio = ps.OutStorageCurrent / Math.Max(ps.OutStorageMax, 1.0f);
            return Loc.GetString("cable-multitool-system-statistics",
                ("supplyc", ps.SupplyCurrent),
                ("supplyb", ps.SupplyBatteries),
                ("supplym", ps.SupplyTheoretical),
                ("consumption", ps.Consumption),
                ("storagec", ps.InStorageCurrent),
                ("storager", storageRatio),
                ("storagem", ps.InStorageMax),
                ("storageoc", ps.OutStorageCurrent),
                ("storageor", outStorageRatio),
                ("storageom", ps.OutStorageMax)
            );
        }

        return Loc.GetString("cable-multitool-system-internal-error-no-power-node");
    }
}
