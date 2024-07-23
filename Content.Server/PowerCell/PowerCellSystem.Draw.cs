using Content.Server.Power.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;

namespace Content.Server.PowerCell;

public sealed partial class PowerCellSystem
{
    /*
     * Handles PowerCellDraw
     */

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<PowerCellDrawComponent, PowerCellSlotComponent, ItemToggleComponent>();

        while (query.MoveNext(out var uid, out var comp, out var slot, out var toggle))
        {
            if (!comp.Enabled || !toggle.Activated)
                continue;

            if (Timing.CurTime < comp.NextUpdateTime)
                continue;

            comp.NextUpdateTime += comp.Delay;

            if (!TryGetBatteryFromSlot(uid, out var batteryEnt, out var battery, slot))
                continue;

            if (_battery.TryUseCharge(batteryEnt.Value, comp.DrawRate, battery))
                continue;

            Toggle.TryDeactivate((uid, toggle));

            var ev = new PowerCellSlotEmptyEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnDrawChargeChanged(EntityUid uid, PowerCellDrawComponent component, ref ChargeChangedEvent args)
    {
        // Update the bools for client prediction.
        var canUse = component.UseRate <= 0f || args.Charge > component.UseRate;

        var canDraw = component.DrawRate <= 0f || args.Charge > 0f;

        if (canUse != component.CanUse || canDraw != component.CanDraw)
        {
            component.CanDraw = canDraw;
            component.CanUse = canUse;
            Dirty(uid, component);
        }
    }

    private void OnDrawCellChanged(EntityUid uid, PowerCellDrawComponent component, PowerCellChangedEvent args)
    {
        var canDraw = !args.Ejected && HasCharge(uid, float.MinValue);
        var canUse = !args.Ejected && HasActivatableCharge(uid, component);

        if (!canDraw)
            Toggle.TryDeactivate(uid);

        if (canUse != component.CanUse || canDraw != component.CanDraw)
        {
            component.CanDraw = canDraw;
            component.CanUse = canUse;
            Dirty(uid, component);
        }
    }
}
