using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        private int _tickTimer = 0;
        private HashSet<GasTileOverlayData> _queue = new HashSet<GasTileOverlayData>();
        private Dictionary<GridId, HashSet<MapIndices>> _invalid = new Dictionary<GridId, HashSet<MapIndices>>();

        private Dictionary<GridId, Dictionary<MapIndices, GasData[]>> _overlay =
            new Dictionary<GridId, Dictionary<MapIndices, GasData[]>>();

        [Robust.Shared.IoC.Dependency] private IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(GridId gridIndex, MapIndices indices)
        {
            if(!_invalid.TryGetValue(gridIndex, out var set) || set == null)
                _invalid.Add(gridIndex, new HashSet<MapIndices>());

            _invalid[gridIndex].Add(indices);
        }

        public void SetTileOverlay(GridId gridIndex, MapIndices indices, GasData[] gasData)
        {
            if(!_overlay.TryGetValue(gridIndex, out var _))
                _overlay[gridIndex] = new Dictionary<MapIndices, GasData[]>();

            _overlay[gridIndex][indices] = gasData;
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
                    if(data.GasData.Length > 0)
                        list.Add(data);
                }
            }

            return list.ToArray();
        }

        private GasTileOverlayData GetData(GridId gridIndex, MapIndices indices)
        {
            return new GasTileOverlayData(gridIndex, indices, _overlay[gridIndex][indices] ?? new GasData[0]);
        }

        private void Revalidate()
        {
            var atmosMan = IoCManager.Resolve<IAtmosphereMap>();
            var list = new List<GasData>();

            foreach (var (gridId, indices) in _invalid)
            {
                var gam = atmosMan.GetGridAtmosphereManager(gridId);

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
                        list.Add(new GasData(i, MathF.Max(MathF.Min(1, moles / Atmospherics.GasMolesVisibleMax), 0f)));
                    }

                    if (list.Count == 0) continue;

                    SetTileOverlay(gridId, index, list.ToArray());
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
