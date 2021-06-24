using System;
using System.Collections.Generic;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class ExcitedGroup : IDisposable
    {
        [ViewVariables]
        private bool _disposed = false;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _tiles = new();

        [ViewVariables]
        private GridAtmosphereComponent _gridAtmosphereComponent = default!;

        [ViewVariables]
        public int DismantleCooldown { get; set; }

        [ViewVariables]
        public int BreakdownCooldown { get; set; }

        public void AddTile(TileAtmosphere tile)
        {
            _tiles.Add(tile);
            tile.ExcitedGroup = this;
            ResetCooldowns();
        }

        public bool RemoveTile(TileAtmosphere tile)
        {
            tile.ExcitedGroup = null;
            return _tiles.Remove(tile);
        }

        public void MergeGroups(ExcitedGroup other)
        {
            var ourSize = _tiles.Count;
            var otherSize = other._tiles.Count;

            if (ourSize > otherSize)
            {
                foreach (var tile in other._tiles)
                {
                    tile.ExcitedGroup = this;
                    _tiles.Add(tile);
                }
                other._tiles.Clear();
                other.Dispose();
                ResetCooldowns();
            }
            else
            {
                foreach (var tile in _tiles)
                {
                    tile.ExcitedGroup = other;
                    other._tiles.Add(tile);
                }
                _tiles.Clear();
                Dispose();
                other.ResetCooldowns();
            }
        }

        ~ExcitedGroup()
        {
            Dispose();
        }

        public void Initialize(GridAtmosphereComponent gridAtmosphereComponent)
        {
            _gridAtmosphereComponent = gridAtmosphereComponent;
            _gridAtmosphereComponent.AddExcitedGroup(this);
        }

        public void ResetCooldowns()
        {
            BreakdownCooldown = 0;
            DismantleCooldown = 0;
        }

        public void SelfBreakdown(AtmosphereSystem atmosphereSystem, bool spaceIsAllConsuming = false)
        {
            var combined = new GasMixture(Atmospherics.CellVolume);

            var tileSize = _tiles.Count;

            if (_disposed) return;

            if (tileSize == 0)
            {
                Dispose();
                return;
            }

            foreach (var tile in _tiles)
            {
                if (tile?.Air == null) continue;
                atmosphereSystem.Merge(combined, tile.Air);
                if (!spaceIsAllConsuming || !tile.Air.Immutable) continue;
                combined.Clear();
                break;
            }

            combined.Multiply(1 / (float)tileSize);

            foreach (var tile in _tiles)
            {
                if (tile?.Air == null) continue;
                tile.Air.CopyFromMutable(combined);
                tile.UpdateVisuals();
            }

            BreakdownCooldown = 0;
        }

        public void Dismantle(bool unexcite = true)
        {
            foreach (var tile in _tiles)
            {
                if (tile == null) continue;
                tile.ExcitedGroup = null;
                if (!unexcite) continue;
                tile.Excited = false;
                _gridAtmosphereComponent.RemoveActiveTile(tile);
            }

            _tiles.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gridAtmosphereComponent.RemoveExcitedGroup(this);

            Dismantle(false);

            _gridAtmosphereComponent = null!;
        }
    }
}
