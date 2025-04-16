using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that damages entities from whitelist in area.
/// </summary>
public sealed class XAEDamageInAreaSystem : BaseXAESystem<XAEDamageInAreaComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entitiesInRange = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEDamageInAreaComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var damageInAreaComponent = ent.Comp;
        _entitiesInRange.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, damageInAreaComponent.Radius, _entitiesInRange);
        foreach (var entityInRange in _entitiesInRange)
        {
            if (!_random.Prob(damageInAreaComponent.DamageChance))
                continue;

            if (_whitelistSystem.IsWhitelistFail(damageInAreaComponent.Whitelist, entityInRange))
                continue;

            _damageable.TryChangeDamage(entityInRange, damageInAreaComponent.Damage, damageInAreaComponent.IgnoreResistances);
        }
    }
}
