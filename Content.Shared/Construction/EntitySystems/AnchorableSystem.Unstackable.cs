using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class AnchorableSystem
{
    /// <summary>
    /// Returns true if any unstackables are also on the corresponding tile.
    /// </summary>
    public bool AnyUnstackable(EntityUid uid, EntityCoordinates location)
    {
        DebugTools.Assert(!Transform(uid).Anchored);

        // If we are unstackable, iterate through any other entities anchored on the current square
        return _tagSystem.HasTag(uid, Unstackable, _tagQuery) && AnyUnstackablesAnchoredAt(location);
    }

    public bool AnyUnstackablesAnchoredAt(EntityCoordinates location)
    {
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(grid.LocalToTile(location));

        while (enumerator.MoveNext(out var entity))
        {
            // If we find another unstackable here, return true.
            if (_tagSystem.HasTag(entity.Value, Unstackable, _tagQuery))
            {
                return true;
            }
        }

        return false;
    }
}
