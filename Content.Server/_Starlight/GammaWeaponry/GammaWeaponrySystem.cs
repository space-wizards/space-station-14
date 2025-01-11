using System.Diagnostics.CodeAnalysis;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Starlight.GammaWeaponry;

public sealed class GammaWeaponrySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _nav = default!;
    
    private const string GammaDockTag = "DockGamma";
    private const string GammaAlertLevel = "gamma";
    
    public override void Initialize()
    {
        SubscribeLocalEvent<GammaWeaponryStationComponent, StationPostInitEvent>(InitializeGammaWeaponryStation);
        SubscribeLocalEvent<GammaWeaponryShuttleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GammaWeaponryShuttleComponent, FTLTagEvent>(SetShuttleTagToDockGamma);
        SubscribeLocalEvent<GammaWeaponryShuttleComponent, FTLCompletedEvent>(AnnounceShuttleDocking);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }
    
    private void InitializeGammaWeaponryStation(EntityUid uid, GammaWeaponryStationComponent comp, StationPostInitEvent ev)
    {
        var map = CreateNewMap();
        if (!TryLoadShuttle(map, new ResPath("Maps/_Starlight/Shuttles/GammaWeaponry.yml"), out var shuttleUids))
            return;

        SetupShuttle(uid, shuttleUids[0], comp);
    }

    private MapId CreateNewMap()
    {
        return _mapManager.CreateMap();
    }

    private bool TryLoadShuttle(MapId map, ResPath path, [NotNullWhen(true)] out IReadOnlyList<EntityUid>? shuttleUids)
    {
        var loadOptions = new MapLoadOptions { LoadMap = true, StoreMapUids = true };
        return _loader.TryLoad(map, path.ToString(), out shuttleUids, loadOptions);
    }

    private void SetupShuttle(EntityUid stationUid, EntityUid shuttleUid, GammaWeaponryStationComponent comp)
    {
        comp.Shuttle = shuttleUid;
        var gammaArmoryComp = EnsureComp<GammaWeaponryShuttleComponent>(shuttleUid);
        gammaArmoryComp.Station = stationUid;
    }
    
    private void OnStartup(EntityUid uid, GammaWeaponryShuttleComponent comp, ComponentStartup ev)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    private void SetShuttleTagToDockGamma(EntityUid uid, GammaWeaponryShuttleComponent comp, ref FTLTagEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        ev.Tag = "DockGamma";
    }
    
    private void AnnounceShuttleDocking(EntityUid uid, GammaWeaponryShuttleComponent comp, ref FTLCompletedEvent ev)
    {
        var xform = Transform(uid);
        var location = FormattedMessage.RemoveMarkup(_nav.GetNearestBeaconString((uid, xform)));

        DispatchAnnouncement("announcement-gamma-armory", location);
    }

    private void DispatchAnnouncement(string messageKey, string location)
    {
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString(messageKey, ("location", location)),
            colorOverride: Color.PaleVioletRed);
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        if (ev.AlertLevel != GammaAlertLevel || !TryComp<GammaWeaponryStationComponent>(ev.Station, out var comp))
            return;

        var targetGrid = _station.GetLargestGrid(Comp<StationDataComponent>(ev.Station));
        
        if (targetGrid == null || comp.Shuttle == null)
            return;

        MoveShuttleToDock(comp.Shuttle.Value, comp.Shuttle.Value, targetGrid.Value);
    }

    private void MoveShuttleToDock(EntityUid shuttle, EntityUid dock, EntityUid target)
    {
        _shuttles.FTLToDock(
            shuttle,
            Comp<ShuttleComponent>(dock),
            target,
            priorityTag: GammaDockTag);
    }
}