using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// <para>Marks a tile's visual overlay as needing to be redetermined.</para>
    ///
    /// <para>A tile's overlay (how it looks like, ex. water vapor's texture)
    /// is determined via determining how much gas there is on the tile.
    /// This is expensive to do for every tile/gas that may have a custom overlay,
    /// so its done once and only updated when it needs to be updated.</para>
    /// </summary>
    /// <param name="grid">The grid the tile is on.</param>
    /// <param name="tile">The tile to invalidate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InvalidateVisuals(Entity<GasTileOverlayComponent?> grid, Vector2i tile)
    {
        _gasTileOverlaySystem.Invalidate(grid, tile);
    }

    /// <summary>
    /// <para>Invalidates a tile on a grid, marking it for revalidation.</para>
    ///
    /// <para>Frequently used tile data like <see cref="AirtightData"/> are determined once and cached.
    /// If this tile's state changes, ex. being added or removed, then this position in the map needs to
    /// be updated.</para>
    ///
    /// <para>Tiles that need to be updated are marked as invalid and revalidated before all other
    /// processing stages.</para>
    /// </summary>
    /// <param name="entity">The grid entity.</param>
    /// <param name="tile">The tile to invalidate.</param>
    [PublicAPI]
    public void InvalidateTile(Entity<GridAtmosphereComponent?> entity, Vector2i tile)
    {
        if (_atmosQuery.Resolve(entity.Owner, ref entity.Comp, false))
            entity.Comp.InvalidatedCoords.Add(tile);
    }
}
