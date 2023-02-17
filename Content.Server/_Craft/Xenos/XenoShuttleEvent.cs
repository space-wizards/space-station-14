using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

using Content.Server.Station.Systems;
using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Robust.Server.Player;
using Content.Server.Players;

namespace Content.Server.StationEvents.Events
{
    internal sealed class XenoShuttle : StationEventSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;

        public override string Prototype => "XenoShuttle";

        private const float ArriveTime = 60.0f;
        private const float DeleteTime = ArriveTime + 10f;
        public const string XenoMapPath = "/Maps/Xenos/XenoStation.yml";

        private bool _prepare;
		private MapId _shuttlemap;
		private MapId _targetmap;
        private EntityUid _shuttle;
        private EntityUid _trgStation;

        public override void Started()
        {
            base.Started();
            _targetmap = GameTicker.DefaultMap;
            EntityUid targetStation = EntityUid.Invalid;
            foreach (var grid in _mapManager.GetAllMapGrids(_targetmap))
            {
                if (!TryComp<StationMemberComponent>(grid.Owner, out var stationMember)) continue;
                if (!TryComp<StationDataComponent>(stationMember.Station, out var stationData)) continue;
                targetStation = stationMember.Station;
                break;
            }
            if (targetStation == EntityUid.Invalid) { ForceEndSelf(); return; };
            _prepare = true;
            _trgStation = targetStation;
            _shuttlemap = _mapManager.CreateMap();
            if (!_map.TryLoad(_shuttlemap, XenoMapPath, out var grids)) { ForceEndSelf(); return; }
            _shuttle = grids[0];
            if (!HasComp<ShuttleComponent>(_shuttle)) AddComp<ShuttleComponent>(_shuttle);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
			if (_prepare) {
				if (Elapsed < ArriveTime) return;
                ArriveToStation();
                _prepare = false;
            } else {
                if (Elapsed < DeleteTime) return;
                ForceEndSelf();
			}
        }

        public override void Ended()
        {
            base.Ended();
            _mapManager.DeleteMap(_shuttlemap);
        }

        private void ArriveToStation()
        {
            if (!HasComp<StationDataComponent>(_trgStation) ||
                !_shuttle.IsValid() ||
                !HasComp<ShuttleComponent>(_shuttle)) return;
            _shuttleSystem.TryFTLDock(Comp<ShuttleComponent>(_shuttle), _station.GetLargestGrid(Comp<StationDataComponent>(_trgStation)).GetValueOrDefault());
            _audioSystem.PlayGlobal("/Audio/Effects/Shuttle/shuttle_impact3.ogg", Filter.BroadcastMap(_targetmap), false);
            _mapManager.SetMapPaused(_shuttlemap, true);
        }
    }
}
