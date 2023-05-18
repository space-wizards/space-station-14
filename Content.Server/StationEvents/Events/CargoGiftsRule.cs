using System.Linq;
using Content.Server.Anomaly;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class CargoGiftsRule : StationEventSystem<CargoGiftsRuleComponent>
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    protected override void Added(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString("cargo-gifts-event-announcement",
            ("sender", component.Sender), ("descr", component.Descr), ("careof", component.Careof));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected override void ActiveTick(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (component.Gifts.Count == 0)
        {
            return;
        }

        if (component.TimeUntilNextGifts > 0)
        {
            component.TimeUntilNextGifts -= frameTime;
            return;
        }

        var station = _stationSystem.Stations.FirstOrNull();
        if (station == null)
        {
            return;
        }

        // TODO: Metric using StationBankAccountComponent
        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoDb))
        {
            return;
        }

        // Add some presents
        int outstanding = _cargoSystem.GetOutstandingOrderCount(cargoDb);
        while (outstanding < cargoDb.Capacity - component.OrderSpaceToLeave && component.Gifts.Count > 0)
        {
            // I wish there was a nice way to pop this
            var (productId, qty) = component.Gifts.First();
            component.Gifts.Remove(productId);

            var id = _cargoSystem.GenerateOrderId(cargoDb);
            var order = new CargoOrderData(id, productId, qty, component.Sender, component.Descr);
            order.SetApproverData(new IdCardComponent(){FullName = component.Careof, JobTitle = component.Sender});
            if (!_cargoSystem.TryAddOrder(cargoDb, order))
            {
                break;
            }

            // Log order addition
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"CargoGiftsRule {component.Descr} added order [orderId:{order.OrderId}, quantity:{order.OrderQuantity}, product:{order.ProductId}, requester:{order.Requester}, reason:{order.Reason}]");

        }

        component.TimeUntilNextGifts = 30f;
    }

}
