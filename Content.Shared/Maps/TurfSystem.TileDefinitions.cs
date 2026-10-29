using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

/// <summary>
/// Tile definition registration and reload handling for <see cref="TurfSystem"/>.
/// </summary>
/// <remarks>
/// Tile IDs are assigned by registration order and are used directly by map chunks. Keep this initialization in a
/// TurfSystem partial so the tile atmosphere cache is always built after registration.
/// </remarks>
public sealed partial class TurfSystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private void RegisterTileDefinitions()
    {
        if (_tileDefinitions.Count > 0)
            return;

        // Register space first because ID 0 is assumed by a few map/tile paths.
        var spaceDef = _prototypeManager.Index<ContentTileDefinition>(ContentTileDefinition.SpaceID);
        _tileDefinitions.Register(spaceDef);

        var prototypeList = new ValueList<ContentTileDefinition>();
        foreach (var tileDef in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            if (tileDef.ID == ContentTileDefinition.SpaceID)
                continue;

            prototypeList.Add(tileDef);
        }

        // Sort ordinal to ensure tile IDs are deterministic and match between client and server.
        prototypeList.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

        foreach (var tileDef in prototypeList)
        {
            _tileDefinitions.Register(tileDef);
        }

        _tileDefinitions.Initialize();
    }

    private void PreserveTileIds()
    {
        // TileDefinitionManager does not re-register definitions on prototype reload. Preserve the IDs assigned during
        // initial registration so reloaded tile prototypes continue to match map chunk tile IDs.
        foreach (var def in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            if (_tileDefinitions.TryGetDefinition(def.ID, out var registered))
                def.AssignTileId(registered.TileId);
        }
    }
}
