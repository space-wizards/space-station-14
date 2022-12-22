using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;

namespace Content.Server.Atmos.EntitySystems;

public struct AtmosObstructionEnumerator
{
    private AnchoredEntitiesEnumerator _enumerator;
    private EntityQuery<AirtightComponent> _query;

    public AtmosObstructionEnumerator(AnchoredEntitiesEnumerator enumerator, EntityQuery<AirtightComponent> query)
    {
        _enumerator = enumerator;
        _query = query;
    }

    public bool MoveNext([NotNullWhen(true)] out AirtightComponent? airtight)
    {
        if (!_enumerator.MoveNext(out var uid))
        {
            airtight = null;
            return false;
        }

        // No rider, it makes it uglier.
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!_query.TryGetComponent(uid.Value, out airtight))
        {
            // ReSharper disable once TailRecursiveCall
            return MoveNext(out airtight);
        }

        return true;
    }
}
