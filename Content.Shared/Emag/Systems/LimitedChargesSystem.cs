using Content.Shared.Emag.Components;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.Emag.Systems;

public sealed class LimitedChargesSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoRechargeComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<LimitedChargesComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (charges, recharge) in EntityQuery<LimitedChargesComponent, AutoRechargeComponent>())
        {
            if (charges.Charges == charges.MaxCharges || _timing.CurTime < recharge.NextChargeTime)
                continue;

            AddCharges(charges, 1);
            recharge.NextChargeTime = _timing.CurTime + recharge.RechargeDuration;
            Dirty(recharge);
        }
    }

    private void OnUnpaused(EntityUid uid, AutoRechargeComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextChargeTime += args.PausedTime;
        Dirty(comp);
    }

    private void OnExamine(EntityUid uid, LimitedChargesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("emag-charges-remaining", ("charges", comp.Charges)));
        if (comp.Charges == comp.MaxCharges)
        {
            args.PushMarkup(Loc.GetString("emag-max-charges"));
            return;
        }

        // only show the recharging info if it's not full
        if (TryComp<AutoRechargeComponent>(uid, out var recharge))
        {
            var timeRemaining = Math.Round((recharge.NextChargeTime - _timing.CurTime).TotalSeconds);
            args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", timeRemaining)));
        }
    }

    /// <summary>
    /// Tries to add a number of charges. If it over or underflows it will be clamped, wasting the extra charges.
    /// </summary>
    public void AddCharges(LimitedChargesComponent comp, int change)
    {
        var old = comp.Charges;
        comp.Charges = Math.Clamp(comp.Charges + change, 0, comp.MaxCharges);
        if (comp.Charges != old)
            Dirty(comp);
    }

    /// <summary>
    /// Gets the limited charges component and returns true if there are no charges. Will return false if there is no limited charges component.
    /// </summary>
    public bool IsEmpty(EntityUid uid, out LimitedChargesComponent? comp)
    {
        comp = null;
        // can't be empty if there are no limited charges
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Charges <= 0;
    }

    /// <summary>
    /// Uses a single charge. Must check IsEmpty beforehand to prevent using with 0 charge.
    /// </summary>
    public void UseCharge(LimitedChargesComponent comp)
    {
        AddCharges(comp, -1);
    }
}
