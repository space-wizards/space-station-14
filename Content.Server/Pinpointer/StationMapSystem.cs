using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.PowerCell;
using Content.Shared.Pinpointer;
using Content.Shared.Station;
using Robust.Server.GameObjects;

namespace Content.Server.Pinpointer;

public sealed class StationMapSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationMapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationMapUserComponent, EntParentChangedMessage>(OnUserParentChanged);

        SubscribeLocalEvent<NukeopsStationMapComponent, ChooseStationMapEvent>(OnNukeOpsStationMap);
        SubscribeLocalEvent<NukeopsTargetStationSelectedEvent>(OnNukeopsStationSelected);

        Subs.BuiEvents<StationMapComponent>(StationMapUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnStationMapOpened);
            subs.Event<BoundUIClosedEvent>(OnStationMapClosed);
        });
    }

    private void OnMapInit(Entity<StationMapComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.InitializeWithStation)
            return;

        var ev = new ChooseStationMapEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
        {
            ent.Comp.TargetGrid = ev.TargetGrid;
            Dirty(ent);
            return;
        }

        var station = _station.GetStationInMap(_xform.GetMapId(ent.Owner));
        if (station != null)
        {
            ent.Comp.TargetGrid = _station.GetLargestGrid((station.Value, null));
            Dirty(ent);
        }
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

    private void OnNukeOpsStationMap(Entity<NukeopsStationMapComponent> entity, ref ChooseStationMapEvent args)
    {
        // If we have this component, we don't want a fallback map!
        args.Handle();

        foreach (var rule in _gameTicker.GetActiveGameRules<NukeopsRuleComponent>())
        {
            if (rule.Comp.TargetStation == null)
                continue;

            args.TargetGrid = _station.GetLargestGrid((rule.Comp.TargetStation.Value, null));
            return;
        }
    }

    private void OnNukeopsStationSelected(ref NukeopsTargetStationSelectedEvent args)
    {
        if (args.TargetStation == null || !TryComp<RuleGridsComponent>(args.RuleEntity, out var ruleGrids))
            return;

        var mapquery = EntityQueryEnumerator<NukeopsStationMapComponent, StationMapComponent>();
        while (mapquery.MoveNext(out var uid, out _, out var map))
        {
            if (Transform(uid).MapID != ruleGrids.Map)
                continue;

            map.TargetGrid = _station.GetLargestGrid((args.TargetStation.Value, null));
            Dirty(uid, map);
        }
    }
}

/// <summary>
/// Selects an alternative target for our station map!
/// If handled, this will not get the map of the current station.
/// </summary>
[ByRefEvent]
public record struct ChooseStationMapEvent
{
    public EntityUid? TargetGrid;
    public bool Handled { get; private set; }

    public void Handle()
    {
        Handled = true;
    }
}
