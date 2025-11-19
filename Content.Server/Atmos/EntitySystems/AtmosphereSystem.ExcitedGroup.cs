using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Handles Excited Groups, an optimization routine executed during LINDA
     that groups active tiles together.

     Groups of active tiles that have very low mole deltas between them
     are dissolved after a cooldown period, performing a final equalization
     on all tiles in the group before deactivating them.

     If tiles are so close together in pressure that the final equalization
     would result in negligible gas transfer, the group is dissolved without
     performing an equalization.

     This prevents LINDA from constantly transferring tiny amounts of gas
     between tiles that are already nearly equalized.
     */

    /// <summary>
    /// Adds a tile to an <see cref="ExcitedGroups"/>, resetting the group's cooldowns in the process.
    /// </summary>
    /// <param name="excitedGroup">The <see cref="ExcitedGroups"/> to add the tile to.</param>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to add.</param>
    private void ExcitedGroupAddTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
    {
        DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
        DebugTools.Assert(tile.ExcitedGroup == null, "Tried to add a tile to an excited group when it's already in another one!");
        excitedGroup.Tiles.Add(tile);
        tile.ExcitedGroup = excitedGroup;
        ExcitedGroupResetCooldowns(excitedGroup);
    }

    /// <summary>
    /// Removes a tile from an <see cref="ExcitedGroups"/>.
    /// </summary>
    /// <param name="excitedGroup">The <see cref="ExcitedGroups"/> to remove the tile from.</param>
    /// <param name="tile">The <see cref="TileAtmosphere"/> to remove.</param>
    private void ExcitedGroupRemoveTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
    {
        DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
        DebugTools.Assert(tile.ExcitedGroup == excitedGroup, "Tried to remove a tile from an excited group it's not present in!");
        tile.ExcitedGroup = null;
        excitedGroup.Tiles.Remove(tile);
    }

    /// <summary>
    /// Merges two <see cref="ExcitedGroups"/>, transferring all tiles from one to the other.
    /// The larger group receives the tiles of the smaller group.
    /// The smaller group is then disposed of without deactivating its tiles.
    /// </summary>
    /// <param name="gridAtmosphere">The <see cref="GridAtmosphereComponent"/> of the grid.</param>
    /// <param name="ourGroup">The first <see cref="ExcitedGroups"/> to merge.</param>
    /// <param name="otherGroup">The second <see cref="ExcitedGroups"/> to merge.</param>
    private void ExcitedGroupMerge(GridAtmosphereComponent gridAtmosphere, ExcitedGroup ourGroup, ExcitedGroup otherGroup)
    {
        DebugTools.Assert(!ourGroup.Disposed, "Excited group is disposed!");
        DebugTools.Assert(!otherGroup.Disposed, "Excited group is disposed!");
        DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(ourGroup), "Grid Atmosphere does not contain Excited Group!");
        DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(otherGroup), "Grid Atmosphere does not contain Excited Group!");
        var ourSize = ourGroup.Tiles.Count;
        var otherSize = otherGroup.Tiles.Count;

        ExcitedGroup winner;
        ExcitedGroup loser;

        if (ourSize > otherSize)
        {
            winner = ourGroup;
            loser = otherGroup;
        }
        else
        {
            winner = otherGroup;
            loser = ourGroup;
        }

        foreach (var tile in loser.Tiles)
        {
            tile.ExcitedGroup = winner;
            winner.Tiles.Add(tile);
        }

        loser.Tiles.Clear();
        ExcitedGroupDispose(gridAtmosphere, loser);
        ExcitedGroupResetCooldowns(winner);
    }

    /// <summary>
    /// Resets the cooldowns of an excited group.
    /// </summary>
    /// <param name="excitedGroup">The <see cref="ExcitedGroups"/> to reset cooldowns for.</param>
    private void ExcitedGroupResetCooldowns(ExcitedGroup excitedGroup)
    {
        DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
        excitedGroup.BreakdownCooldown = 0;
        excitedGroup.DismantleCooldown = 0;
    }

    /// <summary>
    /// Performs a final equalization on all tiles in an excited group before deactivating it.
    /// </summary>
    /// <param name="ent">The grid.</param>
    /// <param name="excitedGroup">The <see cref="ExcitedGroups"/> to equalize and dissolve.</param>
    private void ExcitedGroupSelfBreakdown(
        Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> ent,
        ExcitedGroup excitedGroup)
    {
        DebugTools.Assert(!excitedGroup.Disposed, "Excited group is disposed!");
        DebugTools.Assert(ent.Comp1.ExcitedGroups.Contains(excitedGroup), "Grid Atmosphere does not contain Excited Group!");
        var combined = new GasMixture(Atmospherics.CellVolume);

        var tileSize = excitedGroup.Tiles.Count;

        if (excitedGroup.Disposed)
            return;

        if (tileSize == 0)
        {
            ExcitedGroupDispose(ent.Comp1, excitedGroup);
            return;
        }

        // Combine all gasses in the group into a single mixture
        // for distribution into each individual tile.
        foreach (var tile in excitedGroup.Tiles)
        {
            if (tile?.Air == null)
                continue;

            Merge(combined, tile.Air);

            // If this tile is space and space is all-consuming, the final equalization
            // will result in a vacuum, so we can skip the rest of the equalization.
            if (!ExcitedGroupsSpaceIsAllConsuming || !tile.Space)
                continue;

            combined.Clear();
            break;
        }

        combined.Multiply(1 / (float)tileSize);

        // Distribute the combined mixture evenly to all tiles in the group.
        foreach (var tile in excitedGroup.Tiles)
        {
            if (tile?.Air == null)
                continue;

            tile.Air.CopyFrom(combined);
            InvalidateVisuals(ent, tile);
        }

        excitedGroup.BreakdownCooldown = 0;
    }

    /// <summary>
    /// Deactivates and removes all tiles from an excited group without performing a final equalization.
    /// Used when an excited group is expected to be nearly equalized already to avoid unnecessary processing.
    /// </summary>
    /// <param name="gridAtmosphere">The <see cref="GridAtmosphereComponent"/> of the grid.</param>
    /// <param name="excitedGroup">The <see cref="ExcitedGroups"/> to dissolve.</param>
    private void DeactivateGroupTiles(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup)
    {
        foreach (var tile in excitedGroup.Tiles)
        {
            tile.ExcitedGroup = null;
            RemoveActiveTile(gridAtmosphere, tile);
        }

        excitedGroup.Tiles.Clear();
    }

    /// <summary>
    /// Removes and disposes of an excited group without performing any final equalization
    /// or deactivation of its tiles.
    /// </summary>
    private void ExcitedGroupDispose(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup)
    {
        if (excitedGroup.Disposed)
            return;

        DebugTools.Assert(gridAtmosphere.ExcitedGroups.Contains(excitedGroup), "Grid Atmosphere does not contain Excited Group!");

        excitedGroup.Disposed = true;
        gridAtmosphere.ExcitedGroups.Remove(excitedGroup);

        foreach (var tile in excitedGroup.Tiles)
        {
            tile.ExcitedGroup = null;
        }

        excitedGroup.Tiles.Clear();
    }
}
