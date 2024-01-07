using Content.Server.Cargo.Components;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Interaction.Events;
using YamlDotNet.Core.Tokens;

namespace Content.Server.Cargo.Systems;

public sealed class CargoTrackingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoTrackingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CargoTrackingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CargoTrackingComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CargoTrackedComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CargoOrderArrivalEvent>(OnCargoOrderArrival);
        SubscribeLocalEvent<CargoOrderDeletionEvent>(OnCargoOrderDeletion);
    }

    private void StartWaiting(EntityUid uid, CargoTrackingComponent comp)
    {
        comp.Waiting = true;
        _appearanceSystem.SetData(uid, PinpointerVisuals.IsWaiting, true);
    }

    private void StopWaiting(EntityUid uid, CargoTrackingComponent comp)
    {
        comp.Waiting = false;
        _appearanceSystem.SetData(uid, PinpointerVisuals.IsWaiting, false);
    }

    private void StartTrackingArrival(EntityUid tracker, EntityUid order, CargoTrackingComponent comp, EntityUid? user = null)
    {
        _pinpointerSystem.SetTarget(tracker, order);
        _pinpointerSystem.SetActive(tracker, true);
        if (user is not null)
            _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-start-tracking"), tracker, user.Value);

        // we are no longer waiting (if we were in the first place)
        StopWaiting(tracker, comp);
    }

    private void StopTrackingArrival(EntityUid tracker, EntityUid? user = null)
    {
        _pinpointerSystem.SetTarget(tracker, null);
        _pinpointerSystem.SetActive(tracker, false);
        if (user is not null)
            _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-stop-tracking"), tracker, user.Value);

        if (TryComp<CargoTrackingComponent>(tracker, out var comp))
            comp.TrackedOrderId = null;
    }

    private void OnExamine(EntityUid uid, CargoTrackingComponent comp, ExaminedEvent args)
    {
        if (comp.Waiting)
        {
            args.PushMarkup(Loc.GetString("cargo-tracker-examine-waiting"));
            if (comp.TrackedOrderName is not null)
                args.PushMarkup(Loc.GetString("cargo-tracker-examine-order-name", ("name", comp.TrackedOrderName)));
            return;
        }

        if (comp.TrackedOrderId is not null)
        {
            args.PushMarkup(Loc.GetString("cargo-tracker-examine-tracking"));
            if (comp.TrackedOrderName is not null)
                args.PushMarkup(Loc.GetString("cargo-tracker-examine-order-name", ("name", comp.TrackedOrderName)));
            return;
        }

        args.PushMarkup(Loc.GetString("cargo-tracker-examine-idle"));
    }

    private void OnAfterInteract(EntityUid uid, CargoTrackingComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<CargoInvoiceComponent>(args.Target, out var cargoInvoiceComponent))
            return;

        if (cargoInvoiceComponent.OrderId is null || cargoInvoiceComponent.OrderName is null)
            return;

        component.TrackedOrderId = cargoInvoiceComponent.OrderId;
        component.TrackedOrderName = cargoInvoiceComponent.OrderName;

        var station = _stationSystem.GetOwningStation(uid);
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoOrderDatabaseComponent))
            return;

        // first check if the order is still in the order list
        foreach (var order in cargoOrderDatabaseComponent.Orders)
        {
            if (order.OrderId == cargoInvoiceComponent.OrderId)
            {
                // the order is still out for delivery, so we start waiting
                _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-not-arrived"), uid, args.User);
                StartWaiting(uid, component);
                return;
            }
        }

        // lookup the order from the list
        if (cargoOrderDatabaseComponent.OrderLookup.TryGetValue(component.TrackedOrderId.Value, out var orderUid))
        {
            // start tracking the order
            StartTrackingArrival(uid, orderUid, component, args.User);
            return;
        }

        // if you've gotten this far, yet the order is no longer in the order list or order lookup, it must mean that the tracked entity has been destroyed
        _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-order-gone"), uid, args.User);
    }

    private void OnUseInHand(EntityUid uid, CargoTrackingComponent component, UseInHandEvent args)
    {
        if (component.TrackedOrderId is null)
            return;

        StopTrackingArrival(uid, args.User);
        StopWaiting(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, CargoTrackedComponent component, ComponentRemove args)
    {
        var station = _stationSystem.GetOwningStation(uid);
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoOrderDatabaseComponent))
            return;

        cargoOrderDatabaseComponent.OrderLookup.Remove(component.OrderId);
        RaiseLocalEvent(new CargoOrderDeletionEvent(component.OrderId));
    }

    private void OnCargoOrderArrival(CargoOrderArrivalEvent args)
    {
        // iterating over every order tracker (this is costly, but it doesn't matter because this event is raised so infrequently
        var query = EntityQueryEnumerator<CargoTrackingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TrackedOrderId == args.OrderId)
            {
                StartTrackingArrival(uid, args.OrderEntity, comp);
            }
        }
    }

    private void OnCargoOrderDeletion(CargoOrderDeletionEvent args)
    {
        // iterating over every order tracker (this is costly, but it doesn't matter because this event is raised so infrequently
        var query = EntityQueryEnumerator<CargoTrackingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.TrackedOrderId == args.OrderId)
            {
                StopTrackingArrival(uid);
            }
        }
    }
}

public sealed class CargoOrderArrivalEvent : EventArgs
{
    public int OrderId;
    public EntityUid OrderEntity;

    public CargoOrderArrivalEvent(int orderId, EntityUid orderEntity)
    {
        OrderId = orderId;
        OrderEntity = orderEntity;
    }
}

public sealed class CargoOrderDeletionEvent : EventArgs
{
    public int OrderId;

    public CargoOrderDeletionEvent(int orderId)
    {
        OrderId = orderId;
    }
}

