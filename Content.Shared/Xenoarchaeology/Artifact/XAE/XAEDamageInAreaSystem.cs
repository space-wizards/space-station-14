using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that damages entities from whitelist in area.
/// </summary>
public sealed partial class XAEDamageInAreaSystem : BaseXAESystem<XAEDamageInAreaComponent>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entitiesInRange = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEDamageInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var damage = ent.Comp.Damage;
        if (args.Modifications.TryGetValue(XenoArtifactDamageEffectModifier.DamageAmount, out var amountModifier))
        {
            damage = new DamageSpecifier(damage);
            foreach (var dmg in damage.DamageDict)
            {
                damage.DamageDict[dmg.Key] += (int)amountModifier.Modify(dmg.Value.Value);
            }
        }

        var damageInAreaComponent = ent.Comp;
        _entitiesInRange.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, damageInAreaComponent.Radius, _entitiesInRange);
        foreach (var entityInRange in _entitiesInRange)
        {
            if (_whitelistSystem.IsWhitelistFail(damageInAreaComponent.Whitelist, entityInRange))
                continue;

            var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entityInRange));
            if (!random.Prob(damageInAreaComponent.DamageChance))
                continue;

            _damageable.TryChangeDamage(entityInRange, damage, damageInAreaComponent.IgnoreResistances);
        }
    }
}

public enum XenoArtifactDamageEffectModifier
{
    DamageAmount
}
