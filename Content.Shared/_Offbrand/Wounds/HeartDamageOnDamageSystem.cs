using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Wounds;

public sealed class HeartDamageOnDamageSystem : EntitySystem
{
    [Dependency] private readonly HeartSystem _heart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartDamageOnDamageComponent, DamageChangedEvent>(OnDamageChanged, after: [typeof(WoundableSystem)]);
    }

    private void OnDamageChanged(Entity<HeartDamageOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta || !args.DamageIncreased)
            return;

        var damageable = Comp<DamageableComponent>(ent);
        var heart = Comp<HeartrateComponent>(ent);

        foreach (var threshold in ent.Comp.Thresholds)
        {
            var incomingAmount = ThresholdHelpers.Count(threshold.DamageTypes, delta);
            var totalAmount = ThresholdHelpers.Count(threshold.DamageTypes, damageable.Damage);

            var damageAmount = FixedPoint2.Max(incomingAmount - FixedPoint2.Max(threshold.MinimumTotalDamage - totalAmount, FixedPoint2.Zero), FixedPoint2.Zero);
            var factored = FixedPoint2.New(damageAmount.Double() * threshold.ConversionFactor);

            if (factored <= FixedPoint2.Zero)
                return;

            _heart.ChangeHeartDamage((ent.Owner, heart), factored);
        }
    }
}
