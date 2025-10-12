using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.Wounds;

public sealed class MaximumDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaximumDamageComponent, BeforeDamageCommitEvent>(OnBeforeDamageCommit, before: [typeof(WoundableSystem)]);
    }

    private FixedPoint2 ComputeDelta(FixedPoint2 current, FixedPoint2 incoming, (FixedPoint2 Base, FixedPoint2 Factor) modifier)
    {
        DebugTools.Assert(incoming > 0);

        if (current >= modifier.Base && modifier.Factor != FixedPoint2.Zero)
        {
            var factor = modifier.Factor.Double();
            var @base = modifier.Base.Double();
            Func<FixedPoint2, double> fn = x => Math.Log( Math.Abs(factor - @base + x.Double()) ) * factor;

            var maximumFromNow = FixedPoint2.New(fn(incoming + current) - fn(current));

            return FixedPoint2.Max(incoming - maximumFromNow, FixedPoint2.Zero);
        }
        else if (modifier.Factor != FixedPoint2.Zero)
        {
            var delta = FixedPoint2.Max((incoming + current) - modifier.Base, FixedPoint2.Zero);

            if (delta <= 0)
                return delta;

            var adjustedIncoming = incoming - delta;
            var adjustedCurrent = current + adjustedIncoming;
            var adjustedRemainder = incoming - adjustedIncoming;

            return FixedPoint2.Max( delta - ComputeDelta(adjustedCurrent, adjustedRemainder, modifier), FixedPoint2.Zero );
        }
        else
        {
            return FixedPoint2.Max((incoming + current) - modifier.Base, FixedPoint2.Zero);
        }
    }

    private void OnBeforeDamageCommit(Entity<MaximumDamageComponent> ent, ref BeforeDamageCommitEvent args)
    {
        if (_timing.ApplyingState)
            return;

        var damageable = Comp<DamageableComponent>(ent);

        var dict = damageable.Damage.DamageDict;

        var hasCloned = false;
        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (value <= 0)
                continue;

            if (!dict.TryGetValue(type, out var currentValue))
                continue;

            if (!ent.Comp.Damage.TryGetValue(type, out var maxValue))
                continue;

            var delta = ComputeDelta(currentValue, value, maxValue);

            if (delta <= 0)
                continue;

            if (!hasCloned)
            {
                hasCloned = true;
                args.Damage = new(args.Damage);
            }

            args.Damage.DamageDict[type] -= delta;
        }

        args.Damage.TrimZeros();
    }
}
