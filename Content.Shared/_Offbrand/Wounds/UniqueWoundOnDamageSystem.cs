using Content.Shared.Damage.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class UniqueWoundOnDamageSystem : OffbrandDamageSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UniqueWoundOnDamageComponent, DamageDealtEvent>(OnDamageDealt, after: [typeof(WoundableSystem)]);
    }

    private void OnDamageDealt(Entity<UniqueWoundOnDamageComponent> ent, ref DamageDealtEvent args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var woundable = Comp<WoundableComponent>(ent);

        foreach (var wound in ent.Comp.Wounds)
        {
            var incomingAmount = ThresholdHelpers.Count(wound.DamageTypes, args.Damage);
            var totalAmount = ThresholdHelpers.Count(wound.DamageTypes, woundable.Damage);

            if (incomingAmount < wound.MinimumDamage || totalAmount < wound.MinimumTotalDamage)
                continue;

            var probability = wound.DamageProbabilityCoefficient * incomingAmount.Double() + wound.TotalProbabilityCoefficient * totalAmount.Double() + wound.DamageProbabilityConstant;
            if (!rand.Prob(probability))
                continue;

            _woundable.TryWound((ent.Owner, woundable), wound.WoundPrototype, wound.WoundDamages, unique: true);
        }
    }
}
