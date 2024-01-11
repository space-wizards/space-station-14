using System.Diagnostics.CodeAnalysis;
using Content.Server.Cargo.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Interaction.Events;

namespace Content.Server.Cargo.Systems;

public sealed class CargoTrackingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    private Dictionary<int, List<EntityUid>> _orderTracking = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoTrackingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CargoTrackingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CargoTrackingComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CargoTrackedComponent, EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<CargoOrderArrivalEvent>(OnCargoOrderArrival);
        SubscribeLocalEvent<CargoOrderDeletionEvent>(OnCargoOrderDeletion);
    }

    private void TrackOrder(EntityUid tracker, int orderId)
    {
        if (_orderTracking.TryGetValue(orderId, out var entry))
        {
            if (!entry.Contains(tracker))
                entry.Add(tracker);
        }
        else
        {
            _orderTracking.Add(orderId, new List<EntityUid>() { tracker });
        }
    }

    private bool UntrackOrder(EntityUid tracker, int orderId)
    {
        if (!_orderTracking.TryGetValue(orderId, out var entry) || !entry.Contains(tracker))
            return false;

        entry.Remove(tracker);
        if (entry.Count == 0)
            _orderTracking.Remove(orderId);

        return true;
    }

    private bool UntrackAllOrder(int orderId)
    {
        return _orderTracking.Remove(orderId);
    }

    private bool GetAllTracked(int order, [NotNullWhen(true)] out List<EntityUid>? result)
    {
        if (_orderTracking.TryGetValue(order, out var entry))
        {
            result = entry;
            return true;
        }

        result = null;
        return false;
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
            if (comp.TrackedOrderName is not null && comp.TrackedOrderId is not null)
                args.PushMarkup(Loc.GetString("cargo-tracker-examine-order-name", ("name", comp.TrackedOrderName), ("id", comp.TrackedOrderId)));
            return;
        }

        if (comp.TrackedOrderId is not null)
        {
            args.PushMarkup(Loc.GetString("cargo-tracker-examine-tracking"));
            if (comp.TrackedOrderName is not null)
                args.PushMarkup(Loc.GetString("cargo-tracker-examine-order-name", ("name", comp.TrackedOrderName), ("id", comp.TrackedOrderId)));
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

        component.TrackedOrderId = cargoInvoiceComponent.OrderId;
        component.TrackedOrderName = cargoInvoiceComponent.OrderName;
        TrackOrder(uid, cargoInvoiceComponent.OrderId);

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

    private void OnEntityTerminating(EntityUid uid, CargoTrackedComponent component, EntityTerminatingEvent args)
    {
        var station = _stationSystem.GetOwningStation(uid);
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoOrderDatabaseComponent))
            return;

        cargoOrderDatabaseComponent.OrderLookup.Remove(component.OrderId);
        RaiseLocalEvent(new CargoOrderDeletionEvent(component.OrderId));
    }

    private void OnCargoOrderArrival(CargoOrderArrivalEvent args)
    {
        if (!GetAllTracked(args.OrderId, out var entities))
            return;

        foreach (var entity in entities)
        {
            if (TryComp<CargoTrackingComponent>(entity, out var comp))
            {
                StartTrackingArrival(entity, args.OrderEntity, comp);
            }
        }
    }

    private void OnCargoOrderDeletion(CargoOrderDeletionEvent args)
    {
        if (!GetAllTracked(args.OrderId, out var entities))
            return;

        foreach (var entity in entities)
        {
            StopTrackingArrival(entity);
            UntrackAllOrder(args.OrderId);
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

