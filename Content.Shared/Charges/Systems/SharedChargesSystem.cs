using Content.Shared.Charges.Components;
using Content.Shared.Examine;

namespace Content.Shared.Charges.Systems;

public abstract class SharedChargesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LimitedChargesComponent, ExaminedEvent>(OnExamine);
    }

    protected virtual void OnExamine(EntityUid uid, LimitedChargesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(LimitedChargesComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", comp.Charges)));
            if (comp.Charges == comp.MaxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    /// <summary>
    /// Tries to add a number of charges. If it over or underflows it will be clamped, wasting the extra charges.
    /// </summary>
    public void AddCharges(EntityUid uid, int change, LimitedChargesComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        var old = comp.Charges;
        comp.Charges = Math.Clamp(comp.Charges + change, 0, comp.MaxCharges);
        if (comp.Charges != old)
            Dirty(comp);
    }

    /// <summary>
    /// Gets the limited charges component and returns true if there are no charges. Will return false if there is no limited charges component.
    /// </summary>
    public bool IsEmpty(EntityUid uid, LimitedChargesComponent? comp = null)
    {
        // can't be empty if there are no limited charges
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Charges <= 0;
    }

    /// <summary>
    /// Uses a single charge. Must check IsEmpty beforehand to prevent using with 0 charge.
    /// </summary>
    public virtual void UseCharge(EntityUid uid, LimitedChargesComponent? comp = null)
    {
        if (Resolve(uid, ref comp, false))
            AddCharges(uid, -1, comp);
    }
}
