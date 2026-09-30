using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.EntityTable;

public sealed partial class EntityTableSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Iterates once through Entities that EntitySelector in EntityTableProto provides and picks first one.
    /// </summary>
    public EntProtoId? GetFirstOrNull(
        EntityTablePrototype entTableProto,
        IRobustRandom? rand = null,
        EntityTableContext? ctx = null
    )
    {
        return GetSpawns(entTableProto, rand, ctx).FirstOrNull();
    }

    /// <summary>
    /// Iterates once through Entities that EntitySelector provides and picks first one.
    /// </summary>
    public EntProtoId? GetFirstOrNull(
        EntityTableSelector? entTableProto,
        IRobustRandom? rand = null,
        EntityTableContext? ctx = null
    )
    {
        return GetSpawns(entTableProto, rand, ctx).FirstOrNull();
    }

    /// <summary>
    /// Compiles a random list of entity prototypes using constraints.
    /// </summary>
    public IEnumerable<EntProtoId> GetSpawns(EntityTablePrototype entTableProto, IRobustRandom? rand = null, EntityTableContext? ctx = null)
    {
        // convenient
        return GetSpawns(entTableProto.Table, rand, ctx);
    }

    /// <summary>
    /// Compiles a random list of entity prototypes using constraints.
    /// </summary>
    public IEnumerable<EntProtoId> GetSpawns(EntityTableSelector? table, IRobustRandom? rand = null, EntityTableContext? ctx = null)
    {
        if (table == null)
            return new List<EntProtoId>();

        rand ??= _random;
        ctx ??= new EntityTableContext();
        return table.GetSpawns(rand, EntityManager, ProtoMan, ctx);
    }

    /// <summary>
    /// Builds a list of all the spawns in an EntityTable as keys, and their modified weights as values.
    /// </summary>
    public IEnumerable<(EntProtoId spawn, double)> ListSpawns(EntityTablePrototype entTableProto, EntityTableContext? ctx = null)
    {
        return ListSpawns(entTableProto.Table, ctx);
    }

    /// <summary>
    /// Builds a list of all the spawns in an EntityTable as keys, and their modified weights as values.
    /// </summary>
    /// <param name="table">Table we're examining</param>
    /// <param name="ctx">Optional extra context</param>
    public IEnumerable<(EntProtoId spawn, double)> ListSpawns(EntityTableSelector? table, EntityTableContext? ctx = null)
    {
        if (table == null)
            return new List<(EntProtoId spawn, double)>();

        ctx ??= new EntityTableContext();
        return table.ListSpawns(EntityManager, ProtoMan, ctx);
    }

    /// <inheritdoc cref="AverageSpawns(EntityTableSelector?,EntityTableContext?)"/>
    public IEnumerable<(EntProtoId spawn, double)> AverageSpawns(EntityTablePrototype entTableProto, EntityTableContext? ctx = null)
    {
        return AverageSpawns(entTableProto.Table, ctx);
    }

    /// <summary>
    /// Returns the average expected spawns of a specific entity table.
    /// </summary>
    /// <param name="table">The entity table we want the spawns of</param>
    /// <param name="ctx">Optional EntityTableContext</param>
    /// <returns></returns>
    public IEnumerable<(EntProtoId spawn, double)> AverageSpawns(EntityTableSelector? table, EntityTableContext? ctx = null)
    {
        if (table == null)
            return new List<(EntProtoId spawn, double)>();

        ctx ??= new EntityTableContext();
        return table.AverageSpawns(EntityManager, ProtoMan, ctx);
    }
}
