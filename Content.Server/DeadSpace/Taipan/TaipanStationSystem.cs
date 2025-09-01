// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.DeadSpace.Taipan.Components;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.DeadSpace.Taipan;

public sealed class TaipanStationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private EntityUid _stationGrid = EntityUid.Invalid;
    private MapId _mapId = MapId.Nullspace;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCCCVars.TaipanEnabled, OnValueChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<StationTaipanLoaderComponent, ComponentStartup>(OnStationAdd);
    }

    private void OnStationAdd(Entity<StationTaipanLoaderComponent> ent, ref ComponentStartup args)
    {
        EnsureTaipan(ent);
    }

    private void OnValueChanged(bool obj)
    {
        if (obj)
        {
            var source = EntityQuery<StationTaipanLoaderComponent>().FirstOrDefault();
            if (source == null)
                return;
            EnsureTaipan((source.Owner, source));
        }
        else
        {
            if (_mapId == MapId.Nullspace || !_map.MapExists(_mapId))
                return;

            Del(_stationGrid);
            _map.DeleteMap(_mapId);

            _mapId = MapId.Nullspace;
            _stationGrid = EntityUid.Invalid;
        }
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        Log.Info("OnCleanup");

        if (_mapId == MapId.Nullspace || !_map.MapExists(_mapId))
            return;

        Del(_stationGrid);
        _map.DeleteMap(_mapId);

        _mapId = MapId.Nullspace;
        _stationGrid = EntityUid.Invalid;
    }

    public void EnsureTaipan(Entity<StationTaipanLoaderComponent> source)
    {
        if (!_cfg.GetCVar(CCCCVars.TaipanEnabled))
            return;

        Log.Info("EnsureTaipan");

        if (_stationGrid.IsValid())
        {
            return;
        }

        Log.Info("Start load taipan");

        var opts = DeserializationOptions.Default with { InitializeMaps = true };
        _stationGrid = _gameTicker.LoadGameMap(_prototypeManager.Index(source.Comp.Station), out _mapId, opts).FirstOrNull(HasComp<BecomesStationComponent>)!.Value;
    }
}
