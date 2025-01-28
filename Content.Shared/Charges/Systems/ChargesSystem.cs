using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Charges.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Charges.Systems;

public sealed class ChargesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /*
     * Despite what a bunch of systems do you don't need to continuously tick linear number updates and can just derive it easily.
     */

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedChargesComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<LimitedChargesComponent, ActionAttemptEvent>(OnChargesAttempt);
        SubscribeLocalEvent<LimitedChargesComponent, MapInitEvent>(OnChargesMapInit);
        SubscribeLocalEvent<LimitedChargesComponent, ActionPerformedEvent>(OnChargesPerformed);
    }

    private void OnExamine(EntityUid uid, LimitedChargesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var rechargeEnt = new Entity<LimitedChargesComponent?, AutoRechargeComponent?>(uid, comp, null);
        var charges = GetCurrentCharges(rechargeEnt);
        using var _ = args.PushGroup(nameof(LimitedChargesComponent));

        args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));
        if (GetCurrentCharges((uid, comp, null)) == comp.MaxCharges)
        {
            args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
        }

        // only show the recharging info if it's not full
        if (!args.IsInDetailsRange || charges == comp.MaxCharges || !TryComp<AutoRechargeComponent>(uid, out var recharge))
            return;

        rechargeEnt.Comp2 = recharge;
        var timeRemaining = GetNextRechargeTime(rechargeEnt);
        args.PushMarkup(Loc.GetString("limited-charges-recharging", ("seconds", timeRemaining.TotalSeconds.ToString("F1"))));
    }

    private void OnChargesAttempt(Entity<LimitedChargesComponent> ent, ref ActionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var charges = GetCurrentCharges((ent.Owner, ent.Comp, null));

        if (charges <= 0)
        {
            args.Cancelled = true;
        }
    }

    private void OnChargesPerformed(Entity<LimitedChargesComponent> ent, ref ActionPerformedEvent args)
    {
        AddCharges((ent.Owner, ent.Comp), -1);
    }

    private void OnChargesMapInit(Entity<LimitedChargesComponent> ent, ref MapInitEvent args)
    {
        // If nothing specified use max.
        if (ent.Comp.LastCharges == 0)
        {
            ent.Comp.LastCharges = ent.Comp.MaxCharges;
        }
        // If -1 used then we don't want any.
        else if (ent.Comp.LastCharges < 0)
        {
            ent.Comp.LastCharges = 0;
        }

        ent.Comp.LastUpdate = _timing.CurTime;
        Dirty(ent);
    }

    [Pure]
    public bool HasCharges(Entity<LimitedChargesComponent?> action, int charges)
    {
        var current = GetCurrentCharges(action);

        return current >= charges;
    }

    public void AddCharges(Entity<LimitedChargesComponent?> action, int addCharges)
    {
        if (addCharges == 0)
            return;

        action.Comp ??= EnsureComp<LimitedChargesComponent>(action.Owner);

        var oldCharges = GetCurrentCharges((action.Owner, action.Comp, null));
        var charges = Math.Clamp(oldCharges + addCharges, 0, action.Comp.MaxCharges);

        if (oldCharges == charges)
            return;

        action.Comp.LastCharges = charges;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    public bool TryUseCharge(Entity<LimitedChargesComponent?> entity)
    {
        return TryUseCharges(entity, 1);
    }

    public bool TryUseCharges(Entity<LimitedChargesComponent?> entity, int amount)
    {
        var current = GetCurrentCharges(entity);

        if (current < amount)
        {
            return false;
        }

        AddCharges(entity, -amount);
        return true;
    }

    [Pure]
    public bool IsEmpty(Entity<LimitedChargesComponent?> entity)
    {
        return GetCurrentCharges(entity) == 0;
    }

    /// <summary>
    /// Resets action charges to MaxCharges.
    /// </summary>
    public void ResetCharges(Entity<LimitedChargesComponent?> action)
    {
        if (!Resolve(action.Owner, ref action.Comp, false))
            return;

        var charges = GetCurrentCharges((action.Owner, action.Comp, null));

        if (charges == action.Comp.MaxCharges)
            return;

        action.Comp.LastCharges = action.Comp.MaxCharges;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    public void SetCharges(Entity<LimitedChargesComponent?> action, int value)
    {
        action.Comp ??= EnsureComp<LimitedChargesComponent>(action.Owner);

        var adjusted = Math.Clamp(value, 0, action.Comp.MaxCharges);

        if (action.Comp.LastCharges == adjusted)
        {
            return;
        }

        action.Comp.LastCharges = adjusted;
        action.Comp.LastUpdate = _timing.CurTime;
        Dirty(action);
    }

    [Pure]
    private TimeSpan GetNextRechargeTime(Entity<LimitedChargesComponent?, AutoRechargeComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2, false))
        {
            return TimeSpan.Zero;
        }

        // Okay so essentially we need to get recharge time to full, then modulus that by the recharge timer which should be the next tick.
        var fullTime = ((entity.Comp1.MaxCharges - entity.Comp1.LastCharges) * entity.Comp2.RechargeDuration) + entity.Comp1.LastUpdate;
        var timeRemaining = fullTime - _timing.CurTime;

        if (timeRemaining < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var nextChargeTime = timeRemaining.TotalSeconds % entity.Comp2.RechargeDuration.TotalSeconds;
        return TimeSpan.FromSeconds(nextChargeTime);
    }

    [Pure]
    public int GetCurrentCharges(Entity<LimitedChargesComponent?, AutoRechargeComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, false))
        {
            return 0;
        }

        var calculated = 0;

        if (Resolve(entity.Owner, ref entity.Comp2, false) && entity.Comp2.RechargeDuration.TotalSeconds != 0.0)
        {
            calculated = (int)((_timing.CurTime - entity.Comp1.LastUpdate).TotalSeconds / entity.Comp2.RechargeDuration.TotalSeconds);
        }

        return Math.Clamp(entity.Comp1.LastCharges + calculated,
            0,
            entity.Comp1.MaxCharges);
    }
}
