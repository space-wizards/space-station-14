using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawns from the entity table prototype specified.
/// Can be used to reuse common tables.
/// </summary>
public sealed partial class NestedSelector : EntityTableSelectorWithNestedBase
{
    /// <summary>
    /// The prototype from which to draw random items.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    public override bool CheckConditions(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        return base.CheckConditions(entMan, proto, ctx) &&  proto.Index(TableId).Table.CheckConditions(entMan, proto, ctx);
    }

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(IRobustRandom rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.ListSpawns(entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(IEntityManager entMan, IPrototypeManager proto, EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.AverageSpawns(entMan, proto, ctx);
    }
}
