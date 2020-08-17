using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        private int _tickTimer = 0;
        private HashSet<GasTileOverlayData> _queue = new HashSet<GasTileOverlayData>();
        private Dictionary<GridId, HashSet<MapIndices>> _invalid = new Dictionary<GridId, HashSet<MapIndices>>();

        private Dictionary<GridId, Dictionary<MapIndices, GasOverlayData>> _overlay =
            new Dictionary<GridId, Dictionary<MapIndices, GasOverlayData>>();

        [Robust.Shared.IoC.Dependency] private IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(GridId gridIndex, MapIndices indices)
        {
            if (!_invalid.TryGetValue(gridIndex, out var set) || set == null)
            {
                set = new HashSet<MapIndices>();
                _invalid.Add(gridIndex, set);
            }

            set.Add(indices);
        }

        public void SetTileOverlay(GridId gridIndex, MapIndices indices, GasData[] gasData, int fireState = 0, float fireTemperature = 0f)
        {
            if(!_overlay.TryGetValue(gridIndex, out var _))
                _overlay[gridIndex] = new Dictionary<MapIndices, GasOverlayData>();

            _overlay[gridIndex][indices] = new GasOverlayData(fireState, fireTemperature, gasData);
            _queue.Add(GetData(gridIndex, indices));
        }

        private void OnPlayerStatusChanged(object sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame) return;

            RaiseNetworkEvent(new GasTileOverlayMessage(GetData(), true), e.Session.ConnectedClient);
        }

        private GasTileOverlayData[] GetData()
        {
            var list = new List<GasTileOverlayData>();

            foreach (var (gridId, tiles) in _overlay)
            {
                foreach (var (indices, _) in tiles)
                {
                    var data = GetData(gridId, indices);
                    if(data.Data.Gas.Length > 0)
                        list.Add(data);
                }
            }

            return list.ToArray();
        }

        private GasTileOverlayData GetData(GridId gridIndex, MapIndices indices)
        {
            return new GasTileOverlayData(gridIndex, indices, _overlay[gridIndex][indices]);
        }

        private void Revalidate()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            var entityMan = IoCManager.Resolve<IEntityManager>();
            var list = new List<GasData>();

            foreach (var (gridId, indices) in _invalid)
            {
                if (!mapMan.GridExists(gridId))
                {
                    _invalid.Remove(gridId);
                    return;
                }
                var grid = entityMan.GetEntity(mapMan.GetGrid(gridId).GridEntityId);
                if (!grid.TryGetComponent(out GridAtmosphereComponent gam)) continue;

                foreach (var index in indices)
                {
                    var tile = gam.GetTile(index);

                    if (tile?.Air == null) continue;

                    list.Clear();

                    for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                    {
                        var gas = Atmospherics.GetGas(i);
                        var overlay = gas.GasOverlay;
                        if (overlay == null) continue;
                        var moles = tile.Air.Gases[i];
                        if(moles == 0f || moles < gas.GasMolesVisible) continue;
                        list.Add(new GasData(i, MathF.Max(MathF.Min(1, moles / gas.GasMolesVisibleMax), 0f)));
                    }

                    if (list.Count == 0) continue;

                    SetTileOverlay(gridId, index, list.ToArray(), tile.Hotspot.State, tile.Hotspot.Temperature);
                }

                indices.Clear();
            }
        }

        public override void Update(float frameTime)
        {
            _tickTimer++;

            Revalidate();

            if (_tickTimer < 10) return;

            _tickTimer = 0;
            if(_queue.Count > 0)
                RaiseNetworkEvent(new GasTileOverlayMessage(_queue.ToArray()));
            _queue.Clear();
        }
    }
}
