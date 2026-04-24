using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Organs;

public sealed class OrganDamageOnDamageSystem : OffbrandDamageSystem
{
    [Dependency] private readonly DamageableOrganSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganDamageOnDamageComponent, BodyRelayedEvent<DamageChangedEvent>>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<OrganDamageOnDamageComponent> ent, ref BodyRelayedEvent<DamageChangedEvent> args)
    {
        if (!args.Args.DamageIncreased || args.Args.DamageDelta is not { } delta)
            return;

        var damageable = Comp<DamageableComponent>(args.Body);

        foreach (var threshold in ent.Comp.Thresholds)
        {
            var incomingAmount = ThresholdHelpers.Count(threshold.DamageTypes, delta);
            var totalAmount = ThresholdHelpers.Count(threshold.DamageTypes, damageable.Damage);

            var damageAmount = FixedPoint2.Max(incomingAmount - FixedPoint2.Max(threshold.MinimumTotalDamage - totalAmount, FixedPoint2.Zero), FixedPoint2.Zero);
            var factored = FixedPoint2.New(damageAmount.Double() * threshold.ConversionFactor);

            if (factored <= FixedPoint2.Zero)
                return;

            _damageable.ChangeDamage(ent.Owner, factored);
        }
    }
}
