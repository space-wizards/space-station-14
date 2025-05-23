using System.Diagnostics.CodeAnalysis;
using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

public sealed class EntityTableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public IEnumerable<EntProtoId> GetSpawns(EntityTablePrototype entTableProto, System.Random? rand = null, EntityTableContext? ctx = null)
    {
        // convenient
        return GetSpawns(entTableProto.Table, rand);
    }

    public IEnumerable<EntProtoId> GetSpawns(EntityTableSelector? table, System.Random? rand = null, EntityTableContext? ctx = null)
    {
        if (table == null)
            return new List<EntProtoId>();

        rand ??= _random.GetRandom();
        ctx ??= new EntityTableContext();
        return table.GetSpawns(rand, EntityManager, _prototypeManager, ctx);
    }
}

/// <summary>
/// Context used by selectors and conditions to evaluate in generic gamestate information.
/// </summary>
public sealed class EntityTableContext
{
    private readonly Dictionary<string, object> _data = new();

    public EntityTableContext()
    {

    }

    public EntityTableContext(Dictionary<string, object> data)
    {
        _data = data;
    }

    [PublicAPI]
    public bool TryGetData<T>([ForbidLiteral] string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (!_data.TryGetValue(key, out var valueData) || valueData is not T castValueData)
            return false;

        value = castValueData;
        return true;
    }
}
