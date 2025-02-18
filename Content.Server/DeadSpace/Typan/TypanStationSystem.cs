// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.DeadSpace.Typan.Components;
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

namespace Content.Server.DeadSpace.Typan;

public sealed class TypanStationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCCCVars.TypanEnabled, OnValueChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<StationTypanLoaderComponent, ComponentStartup>(OnStationAdd);
    }

    private void OnStationAdd(Entity<StationTypanLoaderComponent> ent, ref ComponentStartup args)
    {
        EnsureTypan(ent);
    }

    private void OnValueChanged(bool obj)
    {
        if (obj)
        {
            var source = EntityQuery<StationTypanLoaderComponent>().FirstOrDefault();
            if (source == null)
                return;
            EnsureTypan((source.Owner, source), true);
        }
        else
        {
            if (_mapId == MapId.Nullspace || !_map.MapExists(_mapId))
                return;
            _map.DeleteMap(_mapId);

            _mapId = MapId.Nullspace;
            _stationGrid = EntityUid.Invalid;
        }
    }

    private EntityUid _stationGrid = EntityUid.Invalid;
    private MapId _mapId = MapId.Nullspace;

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        Log.Info("OnCleanup");
        QueueDel(_stationGrid);
        _stationGrid = EntityUid.Invalid;

        if (_map.MapExists(_mapId))
            _map.DeleteMap(_mapId);

        _mapId = MapId.Nullspace;
    }

    public void EnsureTypan(Entity<StationTypanLoaderComponent> source, bool force = false)
    {
        // if (!force && (_gameTicker.RunLevel != GameRunLevel.InRound || !_cfg.GetCVar(CCCCVars.TypanEnabled)))
        if (!_cfg.GetCVar(CCCCVars.TypanEnabled))
            return;

        Log.Info("EnsureTypan");

        if (_stationGrid.IsValid())
        {
            return;
        }

        Log.Info("Start load typan");

        var opts = DeserializationOptions.Default with { InitializeMaps = true };
        _stationGrid = _gameTicker.LoadGameMap(_prototypeManager.Index(source.Comp.Station), out _mapId, opts).FirstOrNull(HasComp<BecomesStationComponent>)!.Value;
    }
}
