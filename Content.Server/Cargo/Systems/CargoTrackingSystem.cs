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

    private Dictionary<int, HashSet<EntityUid>> _orderTracking = new();

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

    /// <summary>
    /// Pair an entity and order id together. Used by <see cref="OnCargoOrderArrival"/> and <see cref="OnCargoOrderDeletion"/> to easily find the entities of order trackers that are tracking a specific order id.
    /// </summary>
    /// <param name="tracker">The entity uid of the order tracker.</param>
    /// <param name="orderId">The integer id of the order.</param>
    private void TrackOrder(EntityUid tracker, int orderId)
    {
        if (_orderTracking.TryGetValue(orderId, out var entry))
        {
            if (!entry.Contains(tracker))
                entry.Add(tracker);
        }
        else
        {
            _orderTracking.Add(orderId, new HashSet<EntityUid>() { tracker });
        }
    }

    /// <summary>
    /// Used to clean up and remove a pairing of entity and order id, for when an order tracker has stopped tracking a specific order id.
    /// </summary>
    /// <param name="tracker">The entity uid of the order tracker.</param>
    /// <param name="orderId">The integer id of the order.</param>
    /// <returns>True if the pairing can be successfully deleted, false otherwise.</returns>
    private bool UntrackOrder(EntityUid tracker, int orderId)
    {
        if (!_orderTracking.TryGetValue(orderId, out var entry) || !entry.Contains(tracker))
            return false;

        entry.Remove(tracker);
        if (entry.Count == 0)
            _orderTracking.Remove(orderId);

        return true;
    }

    /// <summary>
    /// Remove all pairings of entity and order id, of a specific order id.
    /// </summary>
    /// <param name="orderId">The id of the orders to untrack.</param>
    /// <returns>True if they have been removed, false otherwise.</returns>
    private bool UntrackAllOrder(int orderId)
    {
        return _orderTracking.Remove(orderId);
    }

    /// <summary>
    /// Get a set of all entities that track a specific order.
    /// </summary>
    /// <param name="order">The integer id of the order.</param>
    /// <param name="result">A set of all entities that are tracking that specific order.</param>
    /// <returns>True if the set is found, false otherwise.</returns>
    private bool GetAllTracked(int order, [NotNullWhen(true)] out HashSet<EntityUid>? result)
    {
        if (_orderTracking.TryGetValue(order, out var entry))
        {
            result = entry;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Set an order tracker to display waiting visuals.
    /// </summary>
    /// <param name="uid">Entity uid of the order tracker.</param>
    /// <param name="comp">CargoTrackingComponent of the entity.</param>
    private void StartWaiting(EntityUid uid, CargoTrackingComponent comp)
    {
        comp.Waiting = true;
        _appearanceSystem.SetData(uid, PinpointerVisuals.IsWaiting, true);
    }

    /// <summary>
    /// Set an order tracker to stop displaying waiting visuals.
    /// </summary>
    /// <param name="uid">Entity uid of the order tracker.</param>
    /// <param name="comp">CargoTrackingComponent of the entity.</param>
    private void StopWaiting(EntityUid uid, CargoTrackingComponent comp)
    {
        comp.Waiting = false;
        _appearanceSystem.SetData(uid, PinpointerVisuals.IsWaiting, false);
    }

    /// <summary>
    /// Set the pinpointer component of an order tracker to start tracking the entity of arrived cargo.
    /// </summary>
    /// <param name="tracker">The entity uid of the tracker.</param>
    /// <param name="order">The integer id of the order.</param>
    /// <param name="comp">The CargoTrackingComponent of the order.</param>
    /// <param name="user">The person who has caused the order tracker to start tracking.</param>
    private void StartTrackingArrival(EntityUid tracker, EntityUid order, CargoTrackingComponent comp, EntityUid? user = null)
    {
        _pinpointerSystem.SetTarget(tracker, order);
        _pinpointerSystem.SetActive(tracker, true);
        if (user is not null)
            _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-start-tracking"), tracker, user.Value);

        // we are no longer waiting (if we were in the first place)
        StopWaiting(tracker, comp);
    }

    /// <summary>
    /// Set the pinpointer component of an order tracker back to neutral.
    /// </summary>
    /// <param name="tracker">The entity uid of the order tracker.</param>
    /// <param name="user">The person who has caused the order tracker to stop tracking.</param>
    private void StopTrackingArrival(EntityUid tracker, EntityUid? user = null)
    {
        _pinpointerSystem.SetTarget(tracker, null);
        _pinpointerSystem.SetActive(tracker, false);
        if (user is not null)
            _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-stop-tracking"), tracker, user.Value);

        if (TryComp<CargoTrackingComponent>(tracker, out var comp))
            comp.TrackedOrderId = null;
    }

    /// <summary>
    /// Show the current state of the order tracker when examined.
    /// </summary>
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

    /// <summary>
    /// Have an order tracker attempt to track an order when it is used on an invoice.
    /// </summary>
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

    /// <summary>
    /// Reset an order tracker, making it no longer track an order.
    /// </summary>
    private void OnUseInHand(EntityUid uid, CargoTrackingComponent component, UseInHandEvent args)
    {
        if (component.TrackedOrderId is null)
            return;

        UntrackOrder(uid, component.TrackedOrderId.Value);
        StopTrackingArrival(uid, args.User);
        StopWaiting(uid, component);
    }

    /// <summary>
    /// Raise an <see cref="CargoOrderDeletionEvent"/> when a tracked cargo order has been deleted.
    /// </summary>
    private void OnEntityTerminating(EntityUid uid, CargoTrackedComponent component, EntityTerminatingEvent args)
    {
        var station = _stationSystem.GetOwningStation(uid);
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoOrderDatabaseComponent))
            return;

        cargoOrderDatabaseComponent.OrderLookup.Remove(component.OrderId);
        RaiseLocalEvent(new CargoOrderDeletionEvent(component.OrderId));
    }

    /// <summary>
    /// React to a <see cref="CargoOrderArrivalEvent"/> by getting any order trackers currently waiting to track this specific order id to start tracking the spawned in entity.
    /// </summary>
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

    /// <summary>
    /// React to a <see cref="CargoOrderArrivalEvent"/> by getting any order trackers currently waiting to track this specific order id to start tracking the spawned in entity.
    /// </summary>
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

