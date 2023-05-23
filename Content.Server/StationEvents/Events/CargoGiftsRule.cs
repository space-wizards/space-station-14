using System.Linq;
using Content.Server.Anomaly;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

public sealed class CargoGiftsRule : StationEventSystem<CargoGiftsRuleComponent>
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    protected override void Added(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        var str = Loc.GetString(component.Announce,
            ("sender", Loc.GetString(component.Sender)), ("description", Loc.GetString(component.Description)), ("dest", Loc.GetString(component.Dest)));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    /// <summary>
    /// Called on an active gamerule entity in the Update function
    /// </summary>
    protected override void ActiveTick(EntityUid uid, CargoGiftsRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (_gameTiming.CurTime < component.ConsiderNextGiftsAt)
            return;

        if (component.Gifts.Count == 0)
        {
            // All done here.
            ForceEndSelf(uid, gameRule);
        }

        component.ConsiderNextGiftsAt = _gameTiming.CurTime + TimeSpan.FromSeconds(30.0f);

        if (!TryGetRandomStation(out var station, HasComp<StationCargoOrderDatabaseComponent>))
            return;

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var cargoDb))
        {
            return;
        }

        // Add some presents
        var outstanding = _cargoSystem.GetOutstandingOrderCount(cargoDb);
        while (outstanding < cargoDb.Capacity - component.OrderSpaceToLeave && component.Gifts.Count > 0)
        {
            // I wish there was a nice way to pop this
            var (productId, qty) = component.Gifts.First();
            component.Gifts.Remove(productId);

            if (!_cargoSystem.AddAndApproveOrder(cargoDb, productId, qty, Loc.GetString(component.Sender), Loc.GetString(component.Description), Loc.GetString(component.Dest)))
            {
                break;
            }
        }
    }

}
