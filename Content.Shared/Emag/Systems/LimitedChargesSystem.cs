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

        SubscribeLocalEvent<LimitedChargesComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<LimitedChargesComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<LimitedChargesComponent>())
        {
            if (!comp.AutoRecharge)
                continue;

            if (comp.Charges == comp.MaxCharges)
                continue;

            if (_timing.CurTime < comp.NextChargeTime)
                continue;

            ChangeCharge(comp, 1, true);
        }
    }

    private void OnUnpaused(EntityUid uid, LimitedChargesComponent comp, ref EntityUnpausedEvent args)
    {
        comp.NextChargeTime += args.PausedTime;
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

        var timeRemaining = Math.Round((comp.NextChargeTime - _timing.CurTime).TotalSeconds);
        args.PushMarkup(Loc.GetString("emag-recharging", ("seconds", timeRemaining)));
    }

    /// <summary>
    /// Tries to change the charge by an amount. Resets the recharge timer if resetTimer is true or it gets filled.
    /// </summary>
    public bool ChangeCharge(LimitedChargesComponent comp, int change, bool resetTimer)
    {
        if (comp.Charges + change < 0 || comp.Charges + change > comp.MaxCharges)
            return false;

        if (resetTimer || comp.Charges == comp.MaxCharges)
            comp.NextChargeTime = _timing.CurTime + comp.RechargeDuration;

        comp.Charges += change;
        Dirty(comp);
        return true;
    }

    /// <summary>
    /// Gets the limited charges component and returns true if there are no charges. Will return false if there is no limited charges component.
    /// </summary>
    public bool IsEmpty(EntityUid uid, out LimitedChargesComponent? comp)
    {
        // can't be empty if there are no limited charges
        comp = null;
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Charges <= 0;
    }

    /// <summary>
    /// Uses a single charge. Must check IsEmpty beforehand to prevent going into negative.
    /// </summary>
    public void UseCharge(LimitedChargesComponent comp)
    {
        ChangeCharge(comp, -1, false);
    }
}
