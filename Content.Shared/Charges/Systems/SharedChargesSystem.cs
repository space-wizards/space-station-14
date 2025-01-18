using Content.Shared.Charges.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Shared.Charges.Systems;

public abstract class SharedChargesSystem : EntitySystem
{
    protected EntityQuery<LimitedChargesComponent> Query;

    public override void Initialize()
    {
        base.Initialize();

        Query = GetEntityQuery<LimitedChargesComponent>();

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
    public virtual void AddCharges(EntityUid uid, FixedPoint2 change, LimitedChargesComponent? comp = null)
    {
        if (!Query.Resolve(uid, ref comp, false))
            return;

        var old = comp.Charges;
        comp.Charges = FixedPoint2.Clamp(comp.Charges + change, 0, comp.MaxCharges);
        if (comp.Charges != old)
            Dirty(uid, comp);
    }

    /// <summary>
    /// Gets the limited charges component and returns true if there are no charges. Will return false if there is no limited charges component.
    /// </summary>
    public bool IsEmpty(EntityUid uid, LimitedChargesComponent? comp = null)
    {
        // can't be empty if there are no limited charges
        if (!Query.Resolve(uid, ref comp, false))
            return false;

        return comp.Charges <= 0;
    }

    /// <summary>
    /// Uses a single charge. Must check IsEmpty beforehand to prevent using with 0 charge.
    /// </summary>
    public void UseCharge(EntityUid uid, LimitedChargesComponent? comp = null)
    {
        AddCharges(uid, -1, comp);
    }

    /// <summary>
    /// Checks IsEmpty and uses a charge if it isn't empty.
    /// </summary>
    public bool TryUseCharge(Entity<LimitedChargesComponent?> ent)
    {
        if (!Query.Resolve(ent, ref ent.Comp, false))
            return true;

        if (IsEmpty(ent, ent.Comp))
            return false;

        UseCharge(ent, ent.Comp);
        return true;
    }

    /// <summary>
    /// Gets the limited charges component and returns true if the number of charges remaining is less than the specified value.
    /// Will return false if there is no limited charges component.
    /// </summary>
    public bool HasInsufficientCharges(EntityUid uid, FixedPoint2 requiredCharges, LimitedChargesComponent? comp = null)
    {
        // can't be empty if there are no limited charges
        if (!Resolve(uid, ref comp, false))
            return false;

        return comp.Charges < requiredCharges;
    }

    /// <summary>
    /// Uses up a specified number of charges. Must check HasInsufficentCharges beforehand to prevent using with insufficient remaining charges.
    /// </summary>
    public virtual void UseCharges(EntityUid uid, FixedPoint2 chargesUsed, LimitedChargesComponent? comp = null)
    {
        AddCharges(uid, -chargesUsed, comp);
    }
}
