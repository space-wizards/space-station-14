using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Wounds;

public sealed class HeartDamageOnDamageSystem : OffbrandDamageSystem
{
    [Dependency] private readonly HeartSystem _heart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartDamageOnDamageComponent, DamageDealtEvent>(OnDamageDealt, after: [typeof(WoundableSystem)]);
    }

    private void OnDamageDealt(Entity<HeartDamageOnDamageComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        var positive = DamageSpecifier.GetPositive(args.Damage);
        var damageable = Comp<DamageableComponent>(ent);
        var heart = Comp<HeartrateComponent>(ent);

        foreach (var threshold in ent.Comp.Thresholds)
        {
            var incomingAmount = ThresholdHelpers.Count(threshold.DamageTypes, positive);
            var totalAmount = ThresholdHelpers.Count(threshold.DamageTypes, damageable.Damage);

            var damageAmount = FixedPoint2.Max(incomingAmount - FixedPoint2.Max(threshold.MinimumTotalDamage - totalAmount, FixedPoint2.Zero), FixedPoint2.Zero);
            var factored = FixedPoint2.New(damageAmount.Double() * threshold.ConversionFactor);

            if (factored <= FixedPoint2.Zero)
                return;

            _heart.ChangeHeartDamage((ent.Owner, heart), factored);
        }
    }
}
