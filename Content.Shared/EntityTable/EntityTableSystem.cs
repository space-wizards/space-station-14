using System.Diagnostics.CodeAnalysis;
using Content.Shared.EntityTable.EntitySelectors;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.EntityTable;

/// <summary>
/// Provides operations on <see cref="EntityTableSelector"/>s.
/// </summary>
/// <remarks>
/// Operations which "traverse" a tree of <see cref="EntityTableSelector"/>s should be implemented using an
/// <see cref="IEntityTableVisitor{TArgs,TResult}"/>. This keeps the implementation details encapsulated within the
/// visitor class where it can reuse common portions and keeps those details cleanly away from the table data's
/// definitions.
/// </remarks>
/// <seealso cref="GetSpawns(EntityTableSelector?, IRobustRandom?, EntityTableContext?)"/>
public sealed partial class EntityTableSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
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

    /// <summary>
    /// Retrieves an arbitrary piece of data from the context based on a provided key.
    /// </summary>
    /// <param name="key">A string key that corresponds to the value we are searching for. </param>
    /// <param name="value">The value we are trying to extract from the context object</param>
    /// <typeparam name="T">The type of <see cref="value"/> that we are trying to retrieve</typeparam>
    /// <returns>If <see cref="key"/> has a corresponding value of type <see cref="T"/></returns>
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
