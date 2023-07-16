using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.Database;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Handles raising BreakerPopEvent when a power provider exceeds its maximum power.
/// </summary>
public sealed class BreakerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BreakerComponent, PowerNetworkBatteryComponent>();
        while (query.MoveNext(out var uid, out var breaker, out var battery))
        {
            if (battery.CurrentSupply > breaker.Limit)
            {
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"Breaker of {ToPrettyString(uid):battery)} popped from supplying {battery.CurrentSupply} with a breaker limit of {breaker.Limit}");
                RaiseLocalEvent(uid, new BreakerPoppedEvent());
            }
        }
    }
}
