using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.Database;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Handles raising BreakerPopEvent when a power provider exceeds its maximum power.
/// </summary>
public sealed class BreakerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BreakerComponent, PowerNetworkBatteryComponent>();
        while (query.MoveNext(out var uid, out var breaker, out var battery))
        {
            if (battery.CurrentSupply > breaker.Limit)
            {
                // require it be overloaded for a certain time before popping
                if (!breaker.Overloaded)
                {
                    breaker.Overloaded = true;
                    breaker.NextPop = _timing.CurTime + breaker.PopTime;
                }

                if (_timing.CurTime >= breaker.NextPop)
                {
                    _adminLogger.Add(LogType.Action, LogImpact.Low, $"Breaker of {ToPrettyString(uid):battery)} popped from supplying {battery.CurrentSupply} with a breaker limit of {breaker.Limit}");
                    RaiseLocalEvent(uid, new BreakerPoppedEvent());
                }
            }
            else
            {
                breaker.Overloaded = false;
            }
        }
    }
}
