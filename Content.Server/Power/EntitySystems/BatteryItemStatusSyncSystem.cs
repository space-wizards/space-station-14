using Content.Shared.Power.Components;
using Content.Server.Power.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Item;
using Content.Shared.Power;
using Content.Server.Weapons.Ranged.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Automatically adds SharedBatteryItemComponent to items that have batteries,
/// making battery status visible when examining items.
/// Also handles syncing battery charge information from server to client.
/// </summary>
public sealed class BatteryItemStatusSyncSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, MapInitEvent>(OnItemMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SharedBatteryItemComponent>();
        while (enumerator.MoveNext(out var uid, out var batteryItem))
        {
            var infoEvent = new GetBatteryInfoEvent();
            RaiseLocalEvent(uid, ref infoEvent);

            if (!infoEvent.HasBattery)
            {
                RemComp<SharedBatteryItemComponent>(uid);
                continue;
            }

            int percent = (int)(infoEvent.ChargePercent * 100);

            if (percent != batteryItem.ChargePercent)
            {
                batteryItem.ChargePercent = percent;
                Dirty(uid, batteryItem);
            }
        }
    }

    private void OnItemMapInit(EntityUid uid, ItemComponent _, MapInitEvent __)
    {
        if (HasComp<SharedBatteryItemComponent>(uid))
            return;

        if (HasComp<AmmoCounterComponent>(uid))
            return;

        var infoEvent = new GetBatteryInfoEvent();
        RaiseLocalEvent(uid, ref infoEvent);

        if (infoEvent.HasBattery)
        {
            EnsureComp<SharedBatteryItemComponent>(uid);
        }
    }
}
