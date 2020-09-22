using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    public class ExcitedGroup : IDisposable
    {
        [ViewVariables]
        private bool _disposed = false;

        [ViewVariables]
        private readonly HashSet<TileAtmosphere> _tile = new HashSet<TileAtmosphere>();

        [ViewVariables]
        private GridAtmosphereComponent _gridAtmosphereComponent;

        [ViewVariables]
        public int DismantleCooldown { get; set; }

        [ViewVariables]
        public int BreakdownCooldown { get; set; }

        public void AddTile(TileAtmosphere tile)
        {
            _tile.Add(tile);
            tile.ExcitedGroup = this;
            ResetCooldowns();
        }

        public void MergeGroups(ExcitedGroup other)
        {
            var ourSize = _tile.Count;
            var otherSize = other._tile.Count;

            if (ourSize > otherSize)
            {
                foreach (var tile in other._tile)
                {
                    tile.ExcitedGroup = this;
                    _tile.Add(tile);
                }
                other._tile.Clear();
                other.Dispose();
                ResetCooldowns();
            }
            else
            {
                foreach (var tile in _tile)
                {
                    tile.ExcitedGroup = other;
                    other._tile.Add(tile);
                }
                _tile.Clear();
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

        public void SelfBreakdown(bool spaceIsAllConsuming = false)
        {
            var combined = new GasMixture(Atmospherics.CellVolume);

            var tileSize = _tile.Count;

            if (_disposed) return;

            if (tileSize == 0)
            {
                Dispose();
                return;
            }

            foreach (var tile in _tile)
            {
                if (tile?.Air == null) continue;
                combined.Merge(tile.Air);
                if (!spaceIsAllConsuming || !tile.Air.Immutable) continue;
                combined.Clear();
                break;
            }

            combined.Multiply(1 / (float)tileSize);

            foreach (var tile in _tile)
            {
                if (tile?.Air == null) continue;
                tile.Air.CopyFromMutable(combined);
                tile.AtmosCooldown = 0;
                tile.UpdateVisuals();
            }

            BreakdownCooldown = 0;
        }

        public void Dismantle(bool unexcite = true)
        {
            foreach (var tile in _tile)
            {
                if (tile == null) continue;
                tile.ExcitedGroup = null;
                if (!unexcite) continue;
                tile.Excited = false;
                _gridAtmosphereComponent.RemoveActiveTile(tile);
            }

            _tile.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gridAtmosphereComponent.RemoveExcitedGroup(this);

            Dismantle(false);

            _gridAtmosphereComponent = null;
        }
    }
}
