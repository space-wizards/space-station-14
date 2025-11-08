using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Server.Power.EntitySystems;

public sealed partial class BatterySystem
{
    public override float ChangeCharge(Entity<BatteryComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;

        var newValue = Math.Clamp(ent.Comp.CurrentCharge + amount, 0, ent.Comp.MaxCharge);
        var delta = newValue - ent.Comp.CurrentCharge;
        ent.Comp.CurrentCharge = newValue;

        TrySetChargeCooldown(ent.Owner);

        var ev = new ChargeChangedEvent(ent.Comp.CurrentCharge, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref ev);
        return delta;
    }

    public override float UseCharge(Entity<BatteryComponent?> ent, float amount)
    {
        if (amount <= 0 || !Resolve(ent, ref ent.Comp) || ent.Comp.CurrentCharge == 0)
            return 0;

        return ChangeCharge(ent, -amount);
    }

    public override bool TryUseCharge(Entity<BatteryComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false) || amount > ent.Comp.CurrentCharge)
            return false;

        UseCharge(ent, amount);
        return true;
    }

    public override void SetCharge(Entity<BatteryComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldCharge = ent.Comp.CurrentCharge;
        ent.Comp.CurrentCharge = MathHelper.Clamp(value, 0, ent.Comp.MaxCharge);
        if (MathHelper.CloseTo(ent.Comp.CurrentCharge, oldCharge) &&
            !(oldCharge != ent.Comp.CurrentCharge && ent.Comp.CurrentCharge == ent.Comp.MaxCharge))
        {
            return;
        }

        var ev = new ChargeChangedEvent(ent.Comp.CurrentCharge, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref ev);
    }
    public override void SetMaxCharge(Entity<BatteryComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var old = ent.Comp.MaxCharge;
        ent.Comp.MaxCharge = Math.Max(value, 0);
        ent.Comp.CurrentCharge = Math.Min(ent.Comp.CurrentCharge, ent.Comp.MaxCharge);
        if (MathHelper.CloseTo(ent.Comp.MaxCharge, old))
            return;

        var ev = new ChargeChangedEvent(ent.Comp.CurrentCharge, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref ev);
    }

    public override void TrySetChargeCooldown(Entity<BatterySelfRechargerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.AutoRechargePauseTime == TimeSpan.Zero)
            return; // no recharge pause

        if (_timing.CurTime + ent.Comp.AutoRechargePauseTime <= ent.Comp.NextAutoRecharge)
            return; // the current pause is already longer

        SetChargeCooldown(ent, ent.Comp.AutoRechargePauseTime);
    }

    public override void SetChargeCooldown(Entity<BatterySelfRechargerComponent?> ent, TimeSpan cooldown)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.NextAutoRecharge = _timing.CurTime + cooldown;
    }

    /// <summary>
    /// Returns whether the battery is full.
    /// </summary>
    public bool IsFull(Entity<BatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.CurrentCharge >= ent.Comp.MaxCharge;
    }
}
