using System.Linq;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects.VariationPass;

/// <summary>
/// Used for replacing tiles on a grid.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TileReplacementVariationPassEntityEffectSystem : EntityEffectSystem<MapGridComponent, TileReplacementVariationPass>
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;

    protected override void Effect(Entity<MapGridComponent> entity, ref EntityEffectEvent<TileReplacementVariationPass> args)
    {
        var tiles = _map.GetAllTiles(entity, entity).ToList();

        if (args.Effect.ReplaceableTiles != null)
        {
            var variationPass = args.Effect;
            tiles = tiles.Where(tile => variationPass.ReplaceableTiles.Contains(_tileDefManager[tile.Tile.TypeId].ID))
                .ToList();
        }

        var totalTiles = tiles.Count();

        var mod = _random.NextGaussian(args.Effect.TilesPerAverage, args.Effect.TilesPerStdDev);
        var replacementTileCount = Math.Max((int) (totalTiles * (1 / mod)), 0);

        for (var i = 0; i < replacementTileCount; i++)
        {
            if (tiles.Count == 0)
                break;

            var selectedTile = _random.PickAndTake(tiles);
            var tileToSet = _random.Pick(args.Effect.ReplacementTiles);
            _map.SetTile(entity, selectedTile.GridIndices, new Tile(ProtoMan.Index(tileToSet).TileId));
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TileReplacementVariationPass : EntityEffectBase<TileReplacementVariationPass>
{
    /// <summary>
    /// Number of tiles before we replace an entity on average.
    /// </summary>
    [DataField]
    public float TilesPerAverage = 50f;

    /// <summary>
    /// Standard deviation for the randomness selection.
    /// </summary>
    [DataField]
    public float TilesPerStdDev = 7f;

    /// <summary>
    /// The floor tiles that will be replaced. If null, will replace all.
    /// </summary>
    [DataField]
    public List<ProtoId<ContentTileDefinition>>? ReplaceableTiles;

    /// <summary>
    /// The tiles that will be replaced into. Randomly picked from the list.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ContentTileDefinition>> ReplacementTiles = default!;
}
