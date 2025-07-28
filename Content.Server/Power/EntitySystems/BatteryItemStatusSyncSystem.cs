using Content.Shared.Power.Components;
using Content.Shared.Power;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Server-side system that updates <see cref="BatteryItemStatusComponent"/> with current battery charge percent
/// so that it can be displayed on the client item status panel.
/// </summary>
public sealed class BatteryItemStatusSyncSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<BatteryItemStatusComponent>();
        while (enumerator.MoveNext(out var uid, out var status))
        {
            // Retrieve battery info using the existing GetBatteryInfoEvent infrastructure
            var infoEvent = new GetBatteryInfoEvent();
            RaiseLocalEvent(uid, ref infoEvent);

            if (!infoEvent.HasBattery)
                continue;

            int percent = (int)(infoEvent.ChargePercent * 100);

            if (percent != status.ChargePercent)
            {
                status.ChargePercent = percent;
                Dirty(uid, status);
            }
        }
    }
}
