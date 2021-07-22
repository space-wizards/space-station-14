using Content.Server.Atmos.Components;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private void ExcitedGroupAddTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
        {
            excitedGroup.Tiles.Add(tile);
            tile.ExcitedGroup = excitedGroup;
            ExcitedGroupResetCooldowns(excitedGroup);
        }

        private void ExcitedGroupRemoveTile(ExcitedGroup excitedGroup, TileAtmosphere tile)
        {
            tile.ExcitedGroup = null;
            excitedGroup.Tiles.Remove(tile);
        }

        private void ExcitedGroupMerge(GridAtmosphereComponent gridAtmosphere, ExcitedGroup ourGroup, ExcitedGroup otherGroup)
        {
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

        private void ExcitedGroupResetCooldowns(ExcitedGroup excitedGroup)
        {
            excitedGroup.BreakdownCooldown = 0;
            excitedGroup.DismantleCooldown = 0;
        }

        private void ExcitedGroupSelfBreakdown(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup, bool spaceIsAllConsuming = false)
        {
            var combined = new GasMixture(Atmospherics.CellVolume);

            var tileSize = excitedGroup.Tiles.Count;

            if (excitedGroup.Disposed) return;

            if (tileSize == 0)
            {
                ExcitedGroupDispose(gridAtmosphere, excitedGroup);
                return;
            }

            foreach (var tile in excitedGroup.Tiles)
            {
                if (tile?.Air == null)
                    continue;

                Merge(combined, tile.Air);

                if (!spaceIsAllConsuming || !tile.Air.Immutable)
                    continue;

                combined.Clear();
                break;
            }

            combined.Multiply(1 / (float)tileSize);

            foreach (var tile in excitedGroup.Tiles)
            {
                if (tile?.Air == null) continue;
                tile.Air.CopyFromMutable(combined);
                InvalidateVisuals(tile.GridIndex, tile.GridIndices);
            }

            excitedGroup.BreakdownCooldown = 0;
        }

        private void ExcitedGroupDismantle(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup, bool unexcite = true)
        {
            foreach (var tile in excitedGroup.Tiles)
            {
                tile.ExcitedGroup = null;

                if (!unexcite)
                    continue;

                RemoveActiveTile(gridAtmosphere, tile);
            }

            excitedGroup.Tiles.Clear();
        }

        private void ExcitedGroupDispose(GridAtmosphereComponent gridAtmosphere, ExcitedGroup excitedGroup)
        {
            if (excitedGroup.Disposed)
                return;

            excitedGroup.Disposed = true;

            gridAtmosphere.ExcitedGroups.Remove(excitedGroup);
            ExcitedGroupDismantle(gridAtmosphere, excitedGroup, false);
        }
    }
}
