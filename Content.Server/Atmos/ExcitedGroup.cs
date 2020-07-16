using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public class ExcitedGroup : IDisposable
    {
        private bool _disposed = false;
        private WeakReference<ExcitedGroup> _weakReference;
        private readonly List<TileAtmosphere> _tile = new List<TileAtmosphere>();
        private IGridAtmosphereManager _gridAtmosphereManager;

        public int DismantleCooldown { get; set; }
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
                for (int i = 0; i < otherSize; i++)
                {
                    var tile = other._tile[i];
                    tile.ExcitedGroup = this;
                    _tile.Add(tile);
                }
                other._tile.Clear();
                ResetCooldowns();
            }
            else
            {
                for (int i = 0; i < ourSize; i++)
                {
                    var tile = _tile[i];
                    tile.ExcitedGroup = other;
                    other._tile.Add(tile);
                }
                _tile.Clear();
                ;
                other.ResetCooldowns();
            }
        }

        public ExcitedGroup()
        {
            _weakReference = new WeakReference<ExcitedGroup>(this);
        }

        ~ExcitedGroup()
        {
            Dispose();
        }

        public void Initialize(IGridAtmosphereManager gridAtmosphereManager)
        {
            _gridAtmosphereManager = gridAtmosphereManager;
            _gridAtmosphereManager.AddExcitedGroup(_weakReference);
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
                combined.Merge(tile.Air);
                if (!spaceIsAllConsuming || !tile.Air.Immutable) continue;
                combined.Clear();
                break;
            }

            combined.Multiply(1 / (float)tileSize);

            foreach (var tile in _tile)
            {
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
                tile.ExcitedGroup = null;
                if (!unexcite) continue;
                tile.Excited = false;
                _gridAtmosphereManager.RemoveActiveTile(tile.GridIndices);
            }

            _tile.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gridAtmosphereManager.RemoveExcitedGroup(_weakReference);
            _gridAtmosphereManager = null;
        }
    }
}
