using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server.GameTicking.Rules;
using Content.Shared.Examine;
using Content.Shared.Lathe;
using Robust.Shared.Prototypes;

namespace Content.Server._Offbrand.VariationPass;

/// <inheritdoc cref="SupplyNearLatheVariationPassComponent"/>
public sealed class SupplyNearLatheVariationPassSystem : VariationPassSystem<SupplyNearLatheVariationPassComponent>
{
    private EntityUid? FindLatheOnStation(EntProtoId proto, EntityUid station)
    {
        var query = AllEntityQuery<LatheComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (MetaData(uid).EntityPrototype?.ID is { } existingProto && existingProto == proto)
                return uid;
        }
        return null;
    }

    protected override void ApplyVariation(Entity<SupplyNearLatheVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        if (FindLatheOnStation(ent.Comp.LathePrototype, args.Station) is not { } lathe)
            return;

        SpawnNextToOrDrop(ent.Comp.EntityToSpawn, lathe);
    }
}
