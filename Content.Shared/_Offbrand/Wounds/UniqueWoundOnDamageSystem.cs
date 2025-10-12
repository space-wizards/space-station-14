using Content.Shared.Damage;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class UniqueWoundOnDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UniqueWoundOnDamageComponent, DamageChangedEvent>(OnDamageChanged, after: [typeof(WoundableSystem)]);
    }

    private void OnDamageChanged(Entity<UniqueWoundOnDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is not { } delta || !args.DamageIncreased)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);

        var damageable = Comp<DamageableComponent>(ent);
        var woundable = Comp<WoundableComponent>(ent);

        foreach (var wound in ent.Comp.Wounds)
        {
            var incomingAmount = ThresholdHelpers.Count(wound.DamageTypes, delta);
            var totalAmount = ThresholdHelpers.Count(wound.DamageTypes, damageable.Damage);

            if (incomingAmount < wound.MinimumDamage || totalAmount < wound.MinimumTotalDamage)
                continue;

            var probability = wound.DamageProbabilityCoefficient * incomingAmount.Double() + wound.DamageProbabilityConstant;
            if (!rand.Prob(probability))
                continue;

            _woundable.TryWound((ent.Owner, woundable), wound.WoundPrototype, wound.WoundDamages, unique: true, refreshDamage: true);
        }
    }
}
