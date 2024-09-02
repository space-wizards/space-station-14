using Content.Server.PowerCell;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Pinpointer;

public sealed class StationMapSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationMapUserComponent, EntParentChangedMessage>(OnUserParentChanged);

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
}
