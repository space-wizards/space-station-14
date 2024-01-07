using Content.Server.Cargo.Components;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Interaction.Events;

namespace Content.Server.Cargo.Systems;

public sealed class CargoTrackingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoTrackingComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CargoTrackingComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CargoTrackedComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CargoOrderArrivalEvent>(OnCargoOrderArrival);
        SubscribeLocalEvent<CargoOrderDeletionEvent>(OnCargoOrderDeletion);
    }

    private void StartTrackingArrival(EntityUid tracker, EntityUid order, CargoTrackingComponent comp, EntityUid? user = null)
    {
        _pinpointerSystem.SetTarget(tracker, order);
        _pinpointerSystem.SetActive(tracker, true);
        if (user is not null)
            _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-start-tracking"), tracker, user.Value);
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

    private void OnAfterInteract(EntityUid uid, CargoTrackingComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<CargoInvoiceComponent>(args.Target, out var cargoInvoiceComponent))
            return;

        if (cargoInvoiceComponent.OrderId is null)
            return;

        component.TrackedOrderId = cargoInvoiceComponent.OrderId;

        var station = _stationSystem.GetOwningStation(uid);
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoOrderDatabaseComponent))
            return;

        // first check if the order is still in the order list
        foreach (var order in cargoOrderDatabaseComponent.Orders)
        {
            if (order.OrderId == cargoInvoiceComponent.OrderId)
            {
                // the order is still out for delivery, so we return early
                _popupSystem.PopupEntity(Loc.GetString("cargo-tracker-not-arrived"), uid, args.User);
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
        // iterate over every order tracker (this is costly, but it doesn't matter because this event is raised so infrequently
        while (EntityQueryEnumerator<CargoTrackingComponent>().MoveNext(out var uid, out var comp))
        {
            if (comp.TrackedOrderId == args.OrderId)
            {
                StartTrackingArrival(uid, args.OrderEntity, comp);
            }
        }
    }

    private void OnCargoOrderDeletion(CargoOrderDeletionEvent args)
    {
        // iterate over every order tracker (this is costly, but it doesn't matter because this event is raised so infrequently
        while (EntityQueryEnumerator<CargoTrackingComponent>().MoveNext(out var uid, out var comp))
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

