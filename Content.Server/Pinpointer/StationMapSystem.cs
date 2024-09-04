using Content.Server.PowerCell;
using Content.Shared.Pinpointer;
using static Content.Shared.Pinpointer.SharedNavMapSystem;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Pinpointer;

public sealed class StationMapSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedNavMapSystem _navMapSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationMapUserComponent, EntParentChangedMessage>(OnUserParentChanged);
        SubscribeLocalEvent<NavMapRegionSeedComponent, MapInitEvent>(OnNavMapBeaconInit);
        SubscribeLocalEvent<NavMapRegionSeedComponent, AnchorStateChangedEvent>(OnNavMapBeaconAnchor);

        Subs.BuiEvents<StationMapComponent>(StationMapUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnStationMapOpened);
            subs.Event<BoundUIClosedEvent>(OnStationMapClosed);
        });
    }

    private void OnStationMapClosed(EntityUid uid, StationMapComponent component, BoundUIClosedEvent args)
    {
        if (!Equals(args.UiKey, StationMapUiKey.Key))
            return;

        RemCompDeferred<StationMapUserComponent>(args.Actor);
    }

    private void OnUserParentChanged(EntityUid uid, StationMapUserComponent component, ref EntParentChangedMessage args)
    {
        _ui.CloseUi(component.Map, StationMapUiKey.Key, uid);
    }

    private void OnStationMapOpened(EntityUid uid, StationMapComponent component, BoundUIOpenedEvent args)
    {
        if (!_cell.TryUseActivatableCharge(uid))
            return;

        var comp = EnsureComp<StationMapUserComponent>(args.Actor);
        comp.Map = uid;
    }

    private void OnNavMapBeaconInit(EntityUid uid, NavMapRegionSeedComponent component, ref MapInitEvent ev)
    {
        OnNavMapBeaconChange(uid);
    }

    private void OnNavMapBeaconAnchor(EntityUid uid, NavMapRegionSeedComponent component, ref AnchorStateChangedEvent ev)
    {
        OnNavMapBeaconChange(uid);
    }

    private void OnNavMapBeaconChange(EntityUid uid)
    {
        var xform = Transform(uid);

        if (!TryComp<NavMapBeaconComponent>(uid, out var beacon) ||
            !TryComp<NavMapComponent>(xform.GridUid, out var navMap))
            return;

        if (xform.Anchored)
        {
            var regionProperties = GetNavMapRegionProperties(uid, beacon);

            //if (regionProperties.HasValue)
            //    _navMapSystem.AddOrUpdateNavMapRegion(xform.GridUid.Value, navMap, GetNetEntity(uid), regionProperties.Value);
        }

        else
        {
            _navMapSystem.RemoveNavMapRegion(xform.GridUid.Value, navMap, GetNetEntity(uid));
        }
    }

    private NavMapRegionProperties? GetNavMapRegionProperties(EntityUid uid, NavMapBeaconComponent component)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
            return null;

        var seeds = new HashSet<Vector2i>()
        {
            _mapSystem.CoordinatesToTile(xform.GridUid.Value, mapGrid, _transformSystem.GetMapCoordinates(uid, xform))
        };

        var regionProperties = new NavMapRegionProperties(GetNetEntity(uid), seeds, component.Color)
        {
            LastUpdate = _gameTiming.CurTick
        };

        return regionProperties;
    }
}
