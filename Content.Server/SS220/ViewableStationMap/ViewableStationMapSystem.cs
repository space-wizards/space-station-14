using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.SS220.ViewableStationMap;

namespace Content.Server.SS220.ViewableStationMap;

public sealed class ViewableStationMapSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ViewableStationMapComponent, BoundUIOpenedEvent>(OnCompUiOpened);
        SubscribeLocalEvent<ViewableStationMapComponent, AnchorStateChangedEvent>(OnAnchored);
    }

    private void OnAnchored(EntityUid entity, ViewableStationMapComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateMap(entity, component);
    }

    private void OnCompUiOpened(EntityUid entity, ViewableStationMapComponent component, BoundUIOpenedEvent args)
    {
        UpdateMap(entity, component);
    }

    private void UpdateMap(EntityUid entity, ViewableStationMapComponent component)
    {
        var station = _station.GetOwningStation(entity);
        if (station == null)
            return;

        if (!TryComp<StationMinimapComponent>(station, out var mapComp))
            return;

        component.MinimapData = mapComp.MinimapData;
        Dirty(entity, component);
    }
}
