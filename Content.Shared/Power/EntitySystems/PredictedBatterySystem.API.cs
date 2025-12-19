using Content.Shared.Power.Components;
using JetBrains.Annotations;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Responsible for <see cref="PredictedBatteryComponent"/>.
/// Predicted equivalent of <see cref="Content.Server.Power.EntitySystems.BatterySystem"/>.
/// If you make changes to this make sure to keep the two consistent.
/// </summary>
public sealed partial class PredictedBatterySystem
{
    /// <summary>
    /// Changes the battery's charge by the given amount
    /// and resets the self-recharge cooldown if it exists.
    /// A positive value will add charge, a negative value will remove charge.
    /// </summary>
    /// <returns>The actually changed amount.</returns>
    [PublicAPI]
    public float ChangeCharge(Entity<PredictedBatteryComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;

        var oldValue = GetCharge(ent);
        var newValue = Math.Clamp(oldValue + amount, 0, ent.Comp.MaxCharge);
        var delta = newValue - oldValue;

        if (delta == 0f)
            return 0f;

        var curTime = _timing.CurTime;
        ent.Comp.LastCharge = newValue;
        ent.Comp.LastUpdate = curTime;
        Dirty(ent);

        TrySetChargeCooldown(ent.Owner);

        var changedEv = new PredictedBatteryChargeChangedEvent(newValue, delta, ent.Comp.ChargeRate, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref changedEv);

        // Raise events if the battery status changed between full, empty, or neither.
        UpdateState(ent);
        return delta;
    }

    /// <summary>
    /// Removes the given amount of charge from the battery
    /// and resets the self-recharge cooldown if it exists.
    /// </summary>
    /// <returns>The actually changed amount.</returns>
    [PublicAPI]
    public float UseCharge(Entity<PredictedBatteryComponent?> ent, float amount)
    {
        if (amount <= 0f)
            return 0f;

        return ChangeCharge(ent, -amount);
    }

    /// <summary>
    /// If sufficient charge is available on the battery, use it. Otherwise, don't.
    /// Resets the self-recharge cooldown if it exists.
    /// Always returns false on the client.
    /// </summary>
    /// <returns>If the full amount was able to be removed.</returns>
    [PublicAPI]
    public bool TryUseCharge(Entity<PredictedBatteryComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, false) || amount > GetCharge(ent))
            return false;

        UseCharge(ent, amount);
        return true;
    }

    /// <summary>
    /// Sets the battery's charge.
    /// </summary>
    [PublicAPI]
    public void SetCharge(Entity<PredictedBatteryComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldValue = GetCharge(ent);
        var newValue = Math.Clamp(value, 0, ent.Comp.MaxCharge);
        var delta = newValue - oldValue;

        if (delta == 0f)
            return;

        var curTime = _timing.CurTime;
        ent.Comp.LastCharge = newValue;
        ent.Comp.LastUpdate = curTime;
        Dirty(ent);

        var ev = new PredictedBatteryChargeChangedEvent(newValue, delta, ent.Comp.ChargeRate, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref ev);

        // Raise events if the battery status changed between full, empty, or neither.
        UpdateState(ent);
    }

    /// <summary>
    /// Sets the battery's maximum charge.
    /// </summary>
    [PublicAPI]
    public void SetMaxCharge(Entity<PredictedBatteryComponent?> ent, float value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (value == ent.Comp.MaxCharge)
            return;

        var oldCharge = GetCharge(ent);
        ent.Comp.MaxCharge = Math.Max(value, 0);
        ent.Comp.LastCharge = GetCharge(ent); // This clamps it using the new max.
        var curTime = _timing.CurTime;
        ent.Comp.LastUpdate = curTime;
        Dirty(ent);

        var ev = new PredictedBatteryChargeChangedEvent(ent.Comp.LastCharge, ent.Comp.LastCharge - oldCharge, ent.Comp.ChargeRate, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref ev);

        // Raise events if the battery status changed between full, empty, or neither.
        UpdateState(ent);
    }

    /// <summary>
    /// Updates the battery's charge state and sends an event if it changed.
    /// </summary>
    [PublicAPI]
    public void UpdateState(Entity<PredictedBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldState = ent.Comp.State;

        var newState = BatteryState.Neither;

        var charge = GetCharge(ent);

        if (charge == ent.Comp.MaxCharge)
            newState = BatteryState.Full;
        else if (charge == 0f)
            newState = BatteryState.Empty;

        if (oldState == newState)
            return;

        ent.Comp.State = newState;
        Dirty(ent);

        var changedEv = new PredictedBatteryStateChangedEvent(oldState, newState);
        RaiseLocalEvent(ent, ref changedEv);
    }

    /// <summary>
    /// Gets the battery's current charge.
    /// </summary>
    [PublicAPI]
    public float GetCharge(Entity<PredictedBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        var curTime = _timing.CurTime;
        // We have a constant charge rate, so the charge changes linearly over time.
        var dt = (curTime - ent.Comp.LastUpdate).TotalSeconds;
        var charge = Math.Clamp(ent.Comp.LastCharge + (float)(dt * ent.Comp.ChargeRate), 0f, ent.Comp.MaxCharge);
        return charge;
    }

    /// <summary>
    /// Gets the fraction of charge remaining (0–1).
    /// </summary>
    [PublicAPI]
    public float GetChargeLevel(Entity<PredictedBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        if (ent.Comp.MaxCharge <= 0f)
            return 0f;

        return GetCharge(ent) / ent.Comp.MaxCharge;
    }

    /// <summary>
    /// Gets number of remaining uses for the given charge cost.
    /// </summary>
    [PublicAPI]
    public int GetRemainingUses(Entity<PredictedBatteryComponent?> ent, float cost)
    {
        if (cost <= 0)
            return 0;

        if (!Resolve(ent, ref ent.Comp))
            return 0;

        return (int)(GetCharge(ent) / cost);
    }

    /// <summary>
    /// Gets number of maximum uses at full charge for the given charge cost.
    /// </summary>
    [PublicAPI]
    public int GetMaxUses(Entity<PredictedBatteryComponent?> ent, float cost)
    {
        if (cost <= 0)
            return 0;

        if (!Resolve(ent, ref ent.Comp))
            return 0;

        return (int)(ent.Comp.MaxCharge / cost);
    }


    /// <summary>
    /// Refreshes the battery's current charge rate by raising a <see cref="RefreshChargeRateEvent"/>.
    /// </summary>
    /// <returns>The new charge rate.</returns>
    [PublicAPI]
    public float RefreshChargeRate(Entity<PredictedBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        ent.Comp.LastCharge = GetCharge(ent); // Prevent the new rate from modifying the current charge.
        var curTime = _timing.CurTime;
        ent.Comp.LastUpdate = curTime;

        var refreshEv = new RefreshChargeRateEvent(ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref refreshEv);
        ent.Comp.ChargeRate = refreshEv.NewChargeRate;
        Dirty(ent);

        // Inform other systems about the new rate;
        var changedEv = new PredictedBatteryChargeChangedEvent(ent.Comp.LastCharge, 0f, ent.Comp.ChargeRate, ent.Comp.MaxCharge);
        RaiseLocalEvent(ent, ref changedEv);

        return refreshEv.NewChargeRate;
    }

    /// <summary>
    /// Checks if the entity has a self recharge and puts it on cooldown if applicable.
    /// Uses the cooldown time given in the component.
    /// </summary>
    [PublicAPI]
    public void TrySetChargeCooldown(Entity<PredictedBatterySelfRechargerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.AutoRechargePauseTime == TimeSpan.Zero)
            return; // no recharge pause

        if (_timing.CurTime + ent.Comp.AutoRechargePauseTime <= ent.Comp.NextAutoRecharge)
            return; // the current pause is already longer

        SetChargeCooldown(ent, ent.Comp.AutoRechargePauseTime);
    }

    /// <summary>
    /// Puts the entity's self recharge on cooldown for the specified time.
    /// </summary>
    [PublicAPI]
    public void SetChargeCooldown(Entity<PredictedBatterySelfRechargerComponent?> ent, TimeSpan cooldown)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.NextAutoRecharge = _timing.CurTime + cooldown;
        Dirty(ent);
        RefreshChargeRate(ent.Owner); // Apply the new charge rate.
    }

    /// <summary>
    /// Returns whether the battery is full.
    /// </summary>
    [PublicAPI]
    public bool IsFull(Entity<PredictedBatteryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return GetCharge(ent) >= ent.Comp.MaxCharge;
    }
}
