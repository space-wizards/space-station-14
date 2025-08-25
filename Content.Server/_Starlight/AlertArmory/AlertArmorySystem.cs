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
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Utility;
using Content.Shared.Station.Components;
using Robust.Shared.Physics.Components;
using System.Numerics;
using System.Linq;

namespace Content.Server.Starlight.AlertArmory;

public sealed class AlertArmorySystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _nav = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    
    private const string GammaDockTag = "DockGamma";
    private const string GammaAlertLevel = "gamma";
    
    public override void Initialize()
    {
        SubscribeLocalEvent<AlertArmoryStationComponent, StationPostInitEvent>(InitializeAlertArmoryStation);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, FTLTagEvent>(SetShuttleTag);
        SubscribeLocalEvent<AlertArmoryShuttleComponent, FTLCompletedEvent>(AnnounceShuttleDocking);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }
    
    ///<summary>
    /// Initialize station with armories and preload all armories.
    ///</summary>
    private void InitializeAlertArmoryStation(EntityUid uid, AlertArmoryStationComponent comp, StationPostInitEvent ev)
    {
        var map = _map.CreateMap(out var mapId);
        _meta.SetEntityName(map, $"AlertArmories {uid}");

        var xOffset = 0f;
        foreach (var (alert, armory) in comp.Shuttles)
        {
            if (!_loader.TryLoadGrid(mapId, armory.Shuttle, out var grid))
            {
                Log.Error($"Failed to load {alert} armory {armory.Shuttle}");
                continue;
            }

            var (gridUid, mapGrid) = grid.Value;

            if (!TryComp<PhysicsComponent>(gridUid, out var physics))
                continue;

            xOffset += mapGrid.LocalAABB.Width / 2;

            var coords = new Vector2(-physics.LocalCenter.X + xOffset, -physics.LocalCenter.Y);
            _transform.SetCoordinates(gridUid, new EntityCoordinates(map, coords));

            xOffset += (mapGrid.LocalAABB.Width / 2) + 1;

            var armoryComp = EnsureComp<AlertArmoryShuttleComponent>(gridUid);
            armoryComp.Station = uid;
            armoryComp.Announcement = armory.Announcement;
            armoryComp.AnnouncementColor = armory.AnnouncementColor;

            comp.Grids[alert] = gridUid;
        }
    }
    
    ///<summary>
    /// Adds component so you can't pilot alert armory and fly off on it
    ///</summary>
    private void OnStartup(EntityUid uid, AlertArmoryShuttleComponent comp, ComponentStartup ev) => EnsureComp<PreventPilotComponent>(uid);
    

    ///<summary>
    /// Sets shuttle dock tag, so it try dock to correct place
    ///</summary>
    private void SetShuttleTag(EntityUid uid, AlertArmoryShuttleComponent comp, ref FTLTagEvent ev)
    {
        if (ev.Handled || comp.DockTag == null)
            return;

        ev.Handled = true;
        ev.Tag = comp.DockTag;
    }
    
    private void AnnounceShuttleDocking(EntityUid uid, AlertArmoryShuttleComponent comp, ref FTLCompletedEvent ev)
    {
        var xform = Transform(uid);
        var location = FormattedMessage.RemoveMarkupPermissive(_nav.GetNearestBeaconString((uid, xform)));

        if (comp.Announcement != null)
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(comp.Announcement, ("location", location)),
                colorOverride: comp.AnnouncementColor ?? Color.PaleVioletRed);
    }

    ///<summary>
    /// Handles alert level changing to try ftl dock gamma alert.
    ///</summary>
    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        if (!TryComp<AlertArmoryStationComponent>(ev.Station, out var comp))
            return;

        var set = comp.Grids.Keys.ToHashSet();
        if (!set.Contains(ev.AlertLevel))
            return;

        var shuttle = comp.Grids[ev.AlertLevel];
        var targetGrid = _station.GetLargestGrid((ev.Station, Comp<StationDataComponent>(ev.Station)));
        
        if (targetGrid == null)
            return;

        _shuttles.FTLToDock(
            shuttle,
            Comp<ShuttleComponent>(shuttle),
            targetGrid.Value,
            priorityTag: GammaDockTag);
    }
}